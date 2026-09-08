<?php
ini_set('display_errors', 0);
ini_set('log_errors', 1);
ini_set('error_log', __DIR__ . '/php-errors.log');
error_reporting(E_ALL);

require_once __DIR__ . '/env.php';
loadEnv();
require_once __DIR__ . '/db.php';

session_start();

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

                case 'remove_participant':
                    $email = $_POST['email'] ?? '';
                    $stmt = $db->prepare('DELETE FROM participants WHERE email = ?');
                    $stmt->bind_param('s', $email);
                    $stmt->execute();
                    $message = "Removed $email.";
                    break;

                case 'reassign_all':
                    $ids = array_column(
                        $db->query('SELECT id FROM participants')->fetch_all(MYSQLI_ASSOC),
                        'id'
                    );
                    shuffle($ids);
                    $groups = CONDITIONS;
                    // Shuffle group order too, so if the count isn't evenly
                    // divisible by 4 the "extra" participant(s) don't always
                    // land in the same condition run after run.
                    shuffle($groups);

                    $stmt = $db->prepare(
                        'UPDATE participants SET condition_group = ?, forced = 0 WHERE id = ?'
                    );
                    foreach ($ids as $i => $id) {
                        $condition = $groups[$i % count($groups)];
                        $stmt->bind_param('si', $condition, $id);
                        $stmt->execute();
                    }
                    $message = 'Reassigned all ' . count($ids) . ' participant(s) into a fresh, evenly balanced 4-way split.';
                    break;
            }
        }

        $participants = $db->query(
            'SELECT email, condition_group, forced, created_at FROM participants ORDER BY created_at DESC'
        )->fetch_all(MYSQLI_ASSOC);
        $counts = countByCondition($db);
        $db->close();
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
                    <option value="">Auto-balance</option>
                    <option value="ML">Force Moderate L</option>
                    <option value="MR">Force Moderate R</option>
                    <option value="EL">Force Extensive L</option>
                    <option value="ER">Force Extensive R</option>
                </select>
            </div>
            <button type="submit" class="btn">Add</button>
        </form>
    </div>

    <div class="panel">
        <div class="panel-header">
            <h2>Participants</h2>
            <a href="participant_admin.php" class="btn btn-small">Show entries</a>
        </div>
        <div class="counts">
            <span>Total: <strong><?= count($participants) ?></strong></span>
            <span>Moderate L: <strong><?= $counts['ML'] ?></strong></span>
            <span>Moderate R: <strong><?= $counts['MR'] ?></strong></span>
            <span>Extensive L: <strong><?= $counts['EL'] ?></strong></span>
            <span>Extensive R: <strong><?= $counts['ER'] ?></strong></span>
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
        <form method="POST" onsubmit="return confirm('This will re-randomize the condition for ALL participants into a fresh, evenly balanced 4-way split (Moderate L / Moderate R / Extensive L / Extensive R), including anyone already assigned. If the study is already in progress, this WILL interfere with collected data. Are you absolutely sure?')">
            <input type="hidden" name="action" value="reassign_all">
            <button type="submit" class="btn danger">Reassign all participants (4-way split)</button>
        </form>
        <p class="danger-zone-note">Re-splits every current participant into a new random, evenly balanced assignment across all 4 conditions and clears any manual "forced" flags. Do not use this once the study has started unless you intend to change existing participants' conditions.</p>
    </div>
</div>

<?php endif; ?>
</body>
</html>
