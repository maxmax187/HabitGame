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
    header('Location: admin.php');
    exit;
}

$authed = isset($_SESSION['admin_auth']) && $_SESSION['admin_auth'] === true;

$participants = [];
$counts = ['L' => 0, 'R' => 0];

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
                        $condition = in_array($force, ['L', 'R'], true)
                            ? $force
                            : pickBalancedCondition(countByCondition($db));
                        $forced = in_array($force, ['L', 'R'], true) ? 1 : 0;

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
                    $half = (int) floor(count($ids) / 2);
                    $firstGroup = random_int(0, 1) === 0 ? 'L' : 'R';
                    $secondGroup = $firstGroup === 'L' ? 'R' : 'L';

                    $stmt = $db->prepare(
                        'UPDATE participants SET condition_group = ?, forced = 0 WHERE id = ?'
                    );
                    foreach ($ids as $i => $id) {
                        $condition = $i < $half ? $firstGroup : $secondGroup;
                        $stmt->bind_param('si', $condition, $id);
                        $stmt->execute();
                    }
                    $message = 'Reassigned all ' . count($ids) . ' participant(s) to a fresh 50/50 split.';
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
            background: #0f0f0f;
            color: #e0e0e0;
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
            background: #1a1a1a;
            border: 1px solid #2a2a2a;
            padding: 2.5rem;
            width: 100%;
            max-width: 420px;
        }

        h1 {
            font-size: 0.75rem;
            letter-spacing: 0.2em;
            text-transform: uppercase;
            color: #666;
            margin-bottom: 2rem;
        }

        h1 span { color: #00ff88; }

        h2 {
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            color: #555;
            margin-bottom: 0.75rem;
        }

        label {
            display: block;
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            color: #666;
            margin-bottom: 0.5rem;
        }

        input[type="password"], input[type="email"], select {
            width: 100%;
            background: #0f0f0f;
            border: 1px solid #333;
            color: #e0e0e0;
            padding: 0.75rem 1rem;
            font-family: inherit;
            font-size: 0.9rem;
            outline: none;
            margin-bottom: 1rem;
        }

        input:focus, select:focus { border-color: #00ff88; }

        .btn {
            background: #00ff88;
            color: #0f0f0f;
            border: none;
            padding: 0.65rem 1.25rem;
            font-family: inherit;
            font-size: 0.7rem;
            letter-spacing: 0.15em;
            text-transform: uppercase;
            cursor: pointer;
            font-weight: bold;
        }

        .btn:hover { background: #00cc6a; }

        .btn.danger { background: #ff4444; }
        .btn.danger:hover { background: #cc2222; }

        a.btn {
            display: inline-block;
            text-decoration: none;
        }

        .btn-small {
            padding: 0.4rem 0.8rem;
            font-size: 0.65rem;
        }

        .panel-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 0.75rem;
        }

        .panel-header h2 { margin-bottom: 0; }

        .error   { font-size: 0.8rem; color: #ff4444; padding: 0.75rem; background: #1a0000; border: 1px solid #440000; margin-bottom: 1rem; }
        .success { font-size: 0.8rem; color: #00ff88; padding: 0.75rem; background: #001a0d; border: 1px solid #004422; margin-bottom: 1rem; }

        .layout { display: flex; flex-direction: column; gap: 1.5rem; max-width: 900px; }

        .panel {
            background: #1a1a1a;
            border: 1px solid #2a2a2a;
            padding: 1.25rem;
        }

        .add-form { display: flex; gap: 0.75rem; align-items: flex-end; flex-wrap: wrap; }
        .add-form .field { flex: 1; min-width: 220px; }
        .add-form label, .add-form input, .add-form select { margin-bottom: 0; }
        .add-form .btn { height: 2.6rem; }

        .counts {
            display: flex;
            gap: 1.5rem;
            font-size: 0.8rem;
            color: #888;
            margin-bottom: 1rem;
        }

        .counts strong { color: #00ff88; }

        table { width: 100%; border-collapse: collapse; font-size: 0.78rem; }

        th {
            background: #111;
            color: #00ff88;
            text-align: left;
            padding: 0.5rem 0.75rem;
            font-size: 0.65rem;
            letter-spacing: 0.1em;
            text-transform: uppercase;
            white-space: nowrap;
            border-bottom: 1px solid #2a2a2a;
        }

        td {
            padding: 0.45rem 0.75rem;
            border-bottom: 1px solid #1f1f1f;
            color: #ccc;
            white-space: nowrap;
        }

        tr:hover td { background: #1f1f1f; }

        .tag {
            display: inline-block;
            padding: 0.1rem 0.5rem;
            font-size: 0.65rem;
            letter-spacing: 0.05em;
            border: 1px solid #333;
        }

        .tag.forced { color: #ffaa00; border-color: #443300; }

        .remove-form { display: inline; }
        .remove-form button {
            background: none;
            border: 1px solid #333;
            color: #ff6666;
            font-family: inherit;
            font-size: 0.65rem;
            padding: 0.2rem 0.6rem;
            cursor: pointer;
            letter-spacing: 0.1em;
            text-transform: uppercase;
        }
        .remove-form button:hover { background: #ff4444; color: #0f0f0f; border-color: #ff4444; }

        .empty { color: #444; font-size: 0.8rem; padding: 1rem 0; }

        .topbar {
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-bottom: 1.5rem;
            max-width: 900px;
        }

        a.logout {
            font-size: 0.65rem;
            color: #444;
            text-decoration: none;
            letter-spacing: 0.1em;
            text-transform: uppercase;
        }

        a.logout:hover { color: #888; }

        .danger-zone-note {
            font-size: 0.7rem;
            color: #666;
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
    <h1 style="margin:0">Participant <span style="color:#00ff88">Admin</span></h1>
    <a href="?logout" class="logout">Log out</a>
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
            <div class="field" style="flex: 0 0 180px;">
                <label for="force_condition">Condition</label>
                <select id="force_condition" name="force_condition">
                    <option value="">Auto-balance</option>
                    <option value="L">Force L</option>
                    <option value="R">Force R</option>
                </select>
            </div>
            <button type="submit" class="btn">Add</button>
        </form>
    </div>

    <div class="panel">
        <div class="panel-header">
            <h2>Participants</h2>
            <a href="admin.php" class="btn btn-small">Show entries</a>
        </div>
        <div class="counts">
            <span>Total: <strong><?= count($participants) ?></strong></span>
            <span>L: <strong><?= $counts['L'] ?></strong></span>
            <span>R: <strong><?= $counts['R'] ?></strong></span>
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
        <form method="POST" onsubmit="return confirm('This will re-randomize the L/R condition for ALL participants into a fresh 50/50 split, including anyone already assigned. If the study is already in progress, this WILL interfere with collected data. Are you absolutely sure?')">
            <input type="hidden" name="action" value="reassign_all">
            <button type="submit" class="btn danger">Reassign all participants (50/50)</button>
        </form>
        <p class="danger-zone-note">Re-splits every current participant into a new random 50/50 L/R assignment and clears any manual "forced" flags. Do not use this once the study has started unless you intend to change existing participants' conditions.</p>
    </div>
</div>

<?php endif; ?>
</body>
</html>
