<?php
ini_set('display_errors', 0);
ini_set('log_errors', 1);
ini_set('error_log', __DIR__ . '/php-errors.log');
error_reporting(E_ALL);

require_once __DIR__ . '/env.php';
loadEnv();
require_once __DIR__ . '/db.php';
require_once __DIR__ . '/import_parser.php';

session_start();

// Display names for the dashboard; keys must match CONDITIONS in db.php.
const CONDITION_LABELS = [
    'MODERATE_REMOVAL' => 'Moderate Removal',
    'MODERATE_DEVALUATION' => 'Moderate Devaluation',
    'EXTENSIVE_REMOVAL' => 'Extensive Removal',
    'EXTENSIVE_DEVALUATION' => 'Extensive Devaluation',
    'SHORT' => 'Short',
];

function conditionOptions(): string {
    $html = '<option value="">Auto-balance</option>';
    foreach (CONDITION_LABELS as $value => $label) {
        $html .= '<option value="' . $value . '">Force ' . $label . '</option>';
    }
    return $html;
}

$error = '';
$message = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['password'])) {
    if (hash_equals((string) getenv('ADMIN_PASSWORD'), $_POST['password'])) {
        $_SESSION['admin_auth'] = true;
    } else {
        $error = 'Incorrect password.';
    }
}

if (isset($_GET['logout'])) {
    session_destroy();
    header('Location: participant_admin.php');
    exit;
}

$authed = isset($_SESSION['admin_auth']) && $_SESSION['admin_auth'] === true;

$participants = [];
$counts = array_fill_keys(CONDITIONS, 0);
$bulk = null;
$bulkEntries = [];
$bulkStatus = [];

if ($authed) {
    try {
        $db = getDb();

        if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action'])) {
            switch ($_POST['action']) {
                case 'add_participant':
                    $email = strtolower(trim($_POST['email'] ?? ''));
                    $force = $_POST['force_condition'] ?? '';

                    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
                        $error = 'Please enter a valid email address.';
                    } else {
                        $condition = in_array($force, CONDITIONS, true)
                            ? $force
                            : pickBalancedCondition(countByCondition($db));
                        $forced = in_array($force, CONDITIONS, true) ? 1 : 0;

                        $stmt = $db->prepare(
                            'INSERT INTO participants (email, condition_group, forced) VALUES (?, ?, ?)'
                        );
                        $stmt->bind_param('ssi', $email, $condition, $forced);

                        if ($stmt->execute()) {
                            $message = "Added $email - assigned condition $condition.";
                        } elseif ($db->errno === 1062) {
                            $error = "$email is already registered.";
                        } else {
                            $error = 'Database error: ' . $db->error;
                        }
                    }
                    break;

                case 'bulk_upload':
                    unset($_SESSION['bulk_import']);
                    $file = $_FILES['import_file'] ?? null;
                    $uploadError = $file['error'] ?? UPLOAD_ERR_NO_FILE;
                    if ($uploadError === UPLOAD_ERR_NO_FILE) {
                        $error = 'Please choose a file to upload.';
                    } elseif ($uploadError === UPLOAD_ERR_INI_SIZE || $uploadError === UPLOAD_ERR_FORM_SIZE
                        || ($uploadError === UPLOAD_ERR_OK && $file['size'] > IMPORT_MAX_BYTES)) {
                        $error = 'The file is too large (max 5 MB).';
                    } elseif ($uploadError !== UPLOAD_ERR_OK || !is_uploaded_file($file['tmp_name'])) {
                        $error = "Upload failed (error code $uploadError).";
                    } else {
                        try {
                            $rows = readSpreadsheet($file['tmp_name'], $file['name']);
                            $columns = importColumns($rows);
                            $column = detectEmailColumn($columns);
                            if ($column === null) {
                                $error = 'No email addresses found in ' . $file['name'] . '.';
                            } else {
                                $_SESSION['bulk_import'] = [
                                    'filename' => $file['name'],
                                    'rows' => $rows,
                                    'columns' => $columns,
                                    'column' => $column,
                                ];
                            }
                        } catch (RuntimeException $e) {
                            $error = $e->getMessage();
                        }
                    }
                    break;

                case 'bulk_column':
                    $col = (int) ($_POST['column'] ?? -1);
                    if (isset($_SESSION['bulk_import']['columns'][$col])) {
                        $_SESSION['bulk_import']['column'] = $col;
                    }
                    break;

                case 'bulk_cancel':
                    unset($_SESSION['bulk_import']);
                    break;

                case 'bulk_import':
                    $import = $_SESSION['bulk_import'] ?? null;
                    $col = (int) ($_POST['column'] ?? -1);
                    if ($import === null) {
                        $error = 'Nothing to import - upload the file again.';
                        break;
                    }
                    if (!isset($import['columns'][$col])) {
                        $error = 'Invalid column selected.';
                        break;
                    }
                    $force = $_POST['force_condition'] ?? '';
                    $forced = in_array($force, CONDITIONS, true) ? 1 : 0;

                    $registered = array_fill_keys(
                        array_column($db->query('SELECT email FROM participants')->fetch_all(MYSQLI_ASSOC), 'email'),
                        true
                    );
                    $entries = buildImportEntries($import['rows'], $col, $registered);
                    $skipped = array_count_values(array_column($entries, 'status'));

                    // Balance as if each participant were added one at a time.
                    $runningCounts = countByCondition($db);
                    $added = array_fill_keys(CONDITIONS, 0);
                    $failed = [];
                    $stmt = $db->prepare(
                        'INSERT INTO participants (email, condition_group, forced) VALUES (?, ?, ?)'
                    );
                    foreach ($entries as $entry) {
                        if ($entry['status'] !== 'new') {
                            continue;
                        }
                        $email = $entry['email'];
                        $condition = $forced ? $force : pickBalancedCondition($runningCounts);
                        $stmt->bind_param('ssi', $email, $condition, $forced);
                        if ($stmt->execute()) {
                            $added[$condition]++;
                            $runningCounts[$condition]++;
                        } elseif ($db->errno === 1062) {
                            $skipped['registered'] = ($skipped['registered'] ?? 0) + 1;
                        } else {
                            $failed[] = "$email ({$db->error})";
                        }
                    }
                    unset($_SESSION['bulk_import']);

                    $parts = [];
                    foreach (array_filter($added) as $condition => $n) {
                        $parts[] = "$condition: $n";
                    }
                    $message = 'Imported ' . array_sum($added) . ' participant(s) from ' . $import['filename']
                        . ($parts ? ' (' . implode(', ', $parts) . ')' : '') . '. Skipped: '
                        . ($skipped['registered'] ?? 0) . ' already registered, '
                        . ($skipped['duplicate'] ?? 0) . ' duplicate(s) in file, '
                        . ($skipped['invalid'] ?? 0) . ' invalid.';
                    if ($failed) {
                        $error = 'Database error for: ' . implode('; ', $failed) . '. ' . $message;
                    }
                    break;

                case 'delete_all_participants':
                    if ($db->query('TRUNCATE TABLE participants')) {
                        $message = 'Deleted all registered participants.';
                    } else {
                        $error = 'Database error: ' . $db->error;
                    }
                    break;

                case 'remove_participant':
                    $email = $_POST['email'] ?? '';
                    $stmt = $db->prepare('DELETE FROM participants WHERE email = ?');
                    $stmt->bind_param('s', $email);
                    $stmt->execute();
                    $message = "Removed $email.";
                    break;

                case 'reassign_all':
                    // SHORT participants are never touched by this - only
                    // participants currently in the balanced pool get
                    // re-shuffled among BALANCED_CONDITIONS.
                    $balancedConditions = BALANCED_CONDITIONS;
                    $placeholders = implode(',', array_fill(0, count($balancedConditions), '?'));
                    $types = str_repeat('s', count($balancedConditions));
                    $stmt = $db->prepare("SELECT id FROM participants WHERE condition_group IN ($placeholders)");
                    $stmt->bind_param($types, ...$balancedConditions);
                    $stmt->execute();
                    $ids = array_column($stmt->get_result()->fetch_all(MYSQLI_ASSOC), 'id');

                    shuffle($ids);
                    $groups = BALANCED_CONDITIONS;
                    // Shuffle group order too, so if the count isn't evenly
                    // divisible by 4 the "extra" participant(s) don't always
                    // land in the same condition run after run.
                    shuffle($groups);

                    $update = $db->prepare(
                        'UPDATE participants SET condition_group = ?, forced = 0 WHERE id = ?'
                    );
                    foreach ($ids as $i => $id) {
                        $condition = $groups[$i % count($groups)];
                        $update->bind_param('si', $condition, $id);
                        $update->execute();
                    }
                    $message = 'Reassigned ' . count($ids) . ' participant(s) into a fresh, evenly balanced 4-way split (SHORT participants were left untouched).';
                    break;
            }
        }

        $participants = $db->query(
            'SELECT email, condition_group, forced, created_at FROM participants ORDER BY created_at DESC'
        )->fetch_all(MYSQLI_ASSOC);
        $counts = countByCondition($db);
        $db->close();

        $bulk = $_SESSION['bulk_import'] ?? null;
        if ($bulk !== null) {
            $bulkEntries = buildImportEntries(
                $bulk['rows'],
                $bulk['column'],
                array_fill_keys(array_column($participants, 'email'), true)
            );
            $bulkStatus = array_count_values(array_column($bulkEntries, 'status'));
        }
    } catch (Throwable $e) {
        $error = $e->getMessage();
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Participant Admin</title>
    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        body {
            font-family: 'Courier New', monospace;
            background: #eef2f8;
            color: #1e293b;
            min-height: 100vh;
            padding: 2rem;
        }

        .login-wrap {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            padding: 2rem;
        }

        .card {
            background: #ffffff;
            border: 1px solid #dbe4ee;
            box-shadow: 0 1px 3px rgba(30, 41, 59, 0.08);
            padding: 2.5rem;
            width: 100%;
            max-width: 420px;
        }

        h1 {
            font-size: 0.75rem;
            letter-spacing: 0.2em;
            text-transform: uppercase;
            color: #64748b;
            margin-bottom: 2rem;
        }

        h1 span { color: #2563eb; }

        h2 {
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            color: #475569;
            margin-bottom: 0.75rem;
        }

        label {
            display: block;
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            color: #64748b;
            margin-bottom: 0.5rem;
        }

        input[type="password"], input[type="email"], select {
            width: 100%;
            background: #ffffff;
            border: 1px solid #cbd5e1;
            color: #1e293b;
            padding: 0.75rem 1rem;
            font-family: inherit;
            font-size: 0.9rem;
            outline: none;
            margin-bottom: 1rem;
        }

        input:focus, select:focus { border-color: #2563eb; }

        .btn {
            background: #2563eb;
            color: #ffffff;
            border: none;
            padding: 0.65rem 1.25rem;
            font-family: inherit;
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            cursor: pointer;
            font-weight: bold;
        }

        .btn:hover { background: #1d4ed8; }

        .btn.danger { background: #dc2626; }
        .btn.danger:hover { background: #b91c1c; }

        a.btn {
            display: inline-block;
            text-decoration: none;
        }

        .btn-small {
            padding: 0.4rem 0.8rem;
            font-size: 0.65rem;
        }

        .btn.secondary {
            background: #ffffff;
            color: #2563eb;
            border: 1px solid #bfdbfe;
        }
        .btn.secondary:hover { background: #eff6ff; }

        .panel-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.75rem;
        }

        .panel-header h2 { margin-bottom: 0; }

        .error   { font-size: 0.8rem; color: #dc2626; padding: 0.75rem; background: #fef2f2; border: 1px solid #fecaca; margin-bottom: 1rem; }
        .success { font-size: 0.8rem; color: #15803d; padding: 0.75rem; background: #f0fdf4; border: 1px solid #bbf7d0; margin-bottom: 1rem; }

        .layout { display: flex; flex-direction: column; gap: 1.5rem; max-width: 900px; }

        .panel {
            background: #ffffff;
            border: 1px solid #dbe4ee;
            box-shadow: 0 1px 3px rgba(30, 41, 59, 0.06);
            padding: 1.25rem;
        }

        .add-form { display: flex; gap: 0.75rem; align-items: flex-end; flex-wrap: wrap; }
        .add-form .field { flex: 1; min-width: 220px; }
        .add-form label, .add-form input, .add-form select { margin-bottom: 0; }
        .add-form .btn { height: 2.6rem; }

        .counts {
            display: flex;
            flex-wrap: wrap;
            gap: 0.75rem 1.5rem;
            font-size: 0.8rem;
            color: #64748b;
            margin-bottom: 1rem;
        }

        .counts strong { color: #2563eb; }

        table { width: 100%; border-collapse: collapse; font-size: 0.78rem; }

        th {
            background: #eff6ff;
            color: #1d4ed8;
            text-align: left;
            padding: 0.5rem 0.75rem;
            font-size: 0.65rem;
            letter-spacing: 0.1em;
            text-transform: uppercase;
            white-space: nowrap;
            border-bottom: 1px solid #dbe4ee;
        }

        td {
            padding: 0.45rem 0.75rem;
            border-bottom: 1px solid #e7edf5;
            color: #334155;
            white-space: nowrap;
        }

        tr:hover td { background: #f5f9ff; }

        .tag {
            display: inline-block;
            padding: 0.1rem 0.5rem;
            font-size: 0.65rem;
            letter-spacing: 0.05em;
            border: 1px solid #cbd5e1;
            color: #64748b;
        }

        .tag.forced { color: #b45309; border-color: #fde68a; background: #fffbeb; }
        .tag.status-new { color: #15803d; border-color: #bbf7d0; background: #f0fdf4; }
        .tag.status-registered, .tag.status-duplicate { color: #64748b; }
        .tag.status-invalid { color: #dc2626; border-color: #fecaca; background: #fef2f2; }

        input[type="file"] {
            width: 100%;
            border: 1px solid #cbd5e1;
            padding: 0.5rem;
            font-family: inherit;
            font-size: 0.8rem;
            background: #ffffff;
        }

        .bulk-summary { font-size: 0.8rem; margin-bottom: 1rem; display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
        .bulk-list { max-height: 320px; overflow-y: auto; border: 1px solid #e7edf5; margin: 1rem 0; }
        .bulk-list th { position: sticky; top: 0; }
        .bulk-actions { margin-bottom: 0.75rem; }
        .btn:disabled { background: #94a3b8; cursor: not-allowed; }

        .remove-form { display: inline; }
        .remove-form button {
            background: none;
            border: 1px solid #fca5a5;
            color: #dc2626;
            font-family: inherit;
            font-size: 0.65rem;
            padding: 0.2rem 0.6rem;
            cursor: pointer;
            letter-spacing: 0.1em;
            text-transform: uppercase;
        }
        .remove-form button:hover { background: #dc2626; color: #ffffff; border-color: #dc2626; }

        .empty { color: #94a3b8; font-size: 0.8rem; padding: 1rem 0; }

        .topbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 1.5rem;
            max-width: 900px;
        }

        .topbar-nav { display: flex; align-items: center; gap: 1rem; }

        a.logout {
            font-size: 0.65rem;
            color: #94a3b8;
            text-decoration: none;
            letter-spacing: 0.1em;
            text-transform: uppercase;
        }

        a.logout:hover { color: #475569; }

        .danger-zone-note {
            font-size: 0.7rem;
            color: #64748b;
            margin-top: 0.75rem;
        }
    </style>
</head>
<body>

<?php if (!$authed): ?>
<div class="login-wrap">
    <div class="card">
        <h1>Participant <span>Admin</span></h1>
        <?php if ($error): ?><div class="error"><?= htmlspecialchars($error) ?></div><?php endif; ?>
        <form method="POST">
            <label for="pw">Password</label>
            <input type="password" id="pw" name="password" autofocus>
            <button type="submit" class="btn">Authenticate</button>
        </form>
    </div>
</div>

<?php else: ?>

<div class="topbar">
    <h1 style="margin:0">Participant <span style="color:#2563eb">Admin</span></h1>
    <div class="topbar-nav">
        <a href="data_admin.php" class="btn secondary btn-small">Game Data</a>
        <a href="?logout" class="logout">Log out</a>
    </div>
</div>

<div class="layout">
    <?php if ($error): ?>
        <div class="error"><?= htmlspecialchars($error) ?></div>
    <?php elseif ($message): ?>
        <div class="success"><?= htmlspecialchars($message) ?></div>
    <?php endif; ?>

    <div class="panel">
        <h2>Add participant</h2>
        <form method="POST" class="add-form">
            <input type="hidden" name="action" value="add_participant">
            <div class="field">
                <label for="email">Email</label>
                <input type="email" id="email" name="email" placeholder="participant@example.com" required>
            </div>
            <div class="field" style="flex: 0 0 200px;">
                <label for="force_condition">Condition</label>
                <select id="force_condition" name="force_condition">
                    <?= conditionOptions() ?>
                </select>
            </div>
            <button type="submit" class="btn">Add</button>
        </form>
    </div>

    <div class="panel">
        <h2>Import participants from file</h2>
        <?php if ($bulk === null): ?>
            <form method="POST" enctype="multipart/form-data" class="add-form">
                <input type="hidden" name="action" value="bulk_upload">
                <div class="field">
                    <label for="import_file">Microsoft Forms export (.xlsx) or .csv / .txt</label>
                    <input type="file" id="import_file" name="import_file" accept=".xlsx,.csv,.txt" required>
                </div>
                <button type="submit" class="btn">Upload &amp; preview</button>
            </form>
            <p class="danger-zone-note">The column holding the email addresses is detected automatically; you can change it in the preview. Nothing is added until you confirm.</p>
        <?php else: ?>
            <p class="bulk-summary">
                <strong><?= htmlspecialchars($bulk['filename'], ENT_SUBSTITUTE) ?></strong> &mdash;
                <span class="tag status-new"><?= $bulkStatus['new'] ?? 0 ?> new</span>
                <span class="tag status-registered"><?= $bulkStatus['registered'] ?? 0 ?> already registered</span>
                <span class="tag status-duplicate"><?= $bulkStatus['duplicate'] ?? 0 ?> duplicate</span>
                <span class="tag status-invalid"><?= $bulkStatus['invalid'] ?? 0 ?> invalid</span>
            </p>

            <form method="POST" class="add-form">
                <input type="hidden" name="action" value="bulk_column">
                <div class="field">
                    <label for="bulk_column">Email column</label>
                    <select id="bulk_column" name="column" onchange="this.form.submit()">
                        <?php foreach ($bulk['columns'] as $i => $col): ?>
                            <option value="<?= $i ?>" <?= $i === $bulk['column'] ? 'selected' : '' ?>>
                                <?= htmlspecialchars($col['label'], ENT_SUBSTITUTE) ?> (<?= $col['emails'] ?> email<?= $col['emails'] === 1 ? '' : 's' ?>)
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <noscript><button type="submit" class="btn secondary">Use column</button></noscript>
            </form>

            <?php if (empty($bulkEntries)): ?>
                <div class="empty">This column has no entries.</div>
            <?php else: ?>
                <div class="bulk-list">
                    <table>
                        <thead>
                            <tr><th>Row</th><th>Email</th><th>Status</th></tr>
                        </thead>
                        <tbody>
                            <?php foreach ($bulkEntries as $entry): ?>
                                <tr>
                                    <td><?= $entry['row'] ?></td>
                                    <td><?= htmlspecialchars($entry['email'], ENT_SUBSTITUTE) ?></td>
                                    <td><span class="tag status-<?= $entry['status'] ?>"><?= $entry['status'] === 'registered' ? 'already registered' : $entry['status'] ?></span></td>
                                </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            <?php endif; ?>

            <form method="POST" class="add-form bulk-actions">
                <input type="hidden" name="action" value="bulk_import">
                <input type="hidden" name="column" value="<?= $bulk['column'] ?>">
                <div class="field" style="flex: 0 0 200px;">
                    <label for="bulk_force">Condition</label>
                    <select id="bulk_force" name="force_condition">
                        <?= conditionOptions() ?>
                    </select>
                </div>
                <button type="submit" class="btn" <?= empty($bulkStatus['new']) ? 'disabled' : '' ?>>Add <?= $bulkStatus['new'] ?? 0 ?> new participant(s)</button>
            </form>
            <form method="POST" class="bulk-cancel">
                <input type="hidden" name="action" value="bulk_cancel">
                <button type="submit" class="btn secondary btn-small">Cancel</button>
            </form>
        <?php endif; ?>
    </div>

    <div class="panel">
        <div class="panel-header">
            <h2>Participants</h2>
            <a href="participant_admin.php" class="btn btn-small">Show entries</a>
        </div>
        <div class="counts">
            <span>Total: <strong><?= count($participants) ?></strong></span>
            <?php foreach (CONDITION_LABELS as $condition => $label): ?>
                <span><?= $label ?>: <strong><?= $counts[$condition] ?? 0 ?></strong></span>
            <?php endforeach; ?>
        </div>

        <?php if (empty($participants)): ?>
            <div class="empty">No participants registered yet.</div>
        <?php else: ?>
            <table>
                <thead>
                    <tr>
                        <th>Email</th>
                        <th>Condition</th>
                        <th>Registered</th>
                        <th></th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($participants as $p): ?>
                        <tr>
                            <td><?= htmlspecialchars($p['email']) ?></td>
                            <td>
                                <?= htmlspecialchars($p['condition_group']) ?>
                                <?php if ($p['forced']): ?><span class="tag forced">forced</span><?php endif; ?>
                            </td>
                            <td><?= htmlspecialchars($p['created_at']) ?></td>
                            <td>
                                <form method="POST" class="remove-form" onsubmit="return confirm('Remove <?= htmlspecialchars($p['email'], ENT_QUOTES) ?>?')">
                                    <input type="hidden" name="action" value="remove_participant">
                                    <input type="hidden" name="email" value="<?= htmlspecialchars($p['email']) ?>">
                                    <button type="submit">Remove</button>
                                </form>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        <?php endif; ?>
    </div>

    <div class="panel">
        <h2>Danger zone</h2>
        <form method="POST" onsubmit="return confirm('This will re-randomize the condition for every Moderate/Extensive participant into a fresh, evenly balanced 4-way split (Moderate Removal / Moderate Devaluation / Extensive Removal / Extensive Devaluation), including anyone already assigned. This can move people between the 1-day and 3-day versions. SHORT participants are left untouched. If the study is already in progress, this WILL interfere with collected data. Are you absolutely sure?')">
            <input type="hidden" name="action" value="reassign_all">
            <button type="submit" class="btn danger">Reassign all participants (4-way split)</button>
        </form>
        <p class="danger-zone-note">Re-splits every participant currently in Moderate/Extensive into a new random, evenly balanced assignment across those 4 conditions and clears any manual "forced" flags. SHORT participants are never included. Do not use this once the study has started unless you intend to change existing participants' conditions.</p>

        <form method="POST" style="margin-top: 1.25rem;" onsubmit="return confirm('This will PERMANENTLY DELETE all <?= count($participants) ?> registered participant(s) in every condition, including SHORT. They will no longer be able to log in to the game. This cannot be undone. Are you absolutely sure?')">
            <input type="hidden" name="action" value="delete_all_participants">
            <button type="submit" class="btn danger">Delete all registered participants</button>
        </form>
        <p class="danger-zone-note">Permanently deletes every row in participants - all conditions, including SHORT. Game data already submitted (Game Data dashboard) is not affected.</p>
    </div>
</div>

<?php endif; ?>
</body>
</html>
