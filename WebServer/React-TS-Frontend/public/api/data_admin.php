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

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['password'])) {
    if (hash_equals((string) getenv('ADMIN_PASSWORD'), $_POST['password'])) {
        $_SESSION['admin_auth'] = true;
    } else {
        $error = 'Incorrect password.';
    }
}

if (isset($_GET['logout'])) {
    session_destroy();
    header('Location: data_admin.php');
    exit;
}

$authed = isset($_SESSION['admin_auth']) && $_SESSION['admin_auth'] === true;

$validViews = ['by_day', 'by_condition', 'cross_tab', 'completion', 'recent'];
$view = $_GET['view'] ?? 'by_day';
if (!in_array($view, $validViews, true)) {
    $view = 'by_day';
}

$summary = ['total' => 0, 'participants_with_data' => 0, 'latest' => null];
$byDay = [];
$byCondition = [];
$crossTab = [];
$completion = [];
$recent = [];

if ($authed) {
    try {
        $db = getDb();

        $summary['total'] = (int) ($db->query('SELECT COUNT(*) AS c FROM game_data')->fetch_assoc()['c'] ?? 0);
        $summary['participants_with_data'] = (int) (
            $db->query('SELECT COUNT(DISTINCT participant_email) AS c FROM game_data')->fetch_assoc()['c'] ?? 0
        );
        $summary['latest'] = $db->query('SELECT MAX(submitted_at) AS latest FROM game_data')->fetch_assoc()['latest'] ?? null;

        switch ($view) {
            case 'by_condition':
                $byCondition = $db->query(
                    'SELECT condition_group, COUNT(*) AS c FROM game_data GROUP BY condition_group ORDER BY condition_group'
                )->fetch_all(MYSQLI_ASSOC);
                break;

            case 'cross_tab':
                $crossTab = $db->query(
                    'SELECT day, condition_group, COUNT(*) AS c FROM game_data GROUP BY day, condition_group ORDER BY day, condition_group'
                )->fetch_all(MYSQLI_ASSOC);
                break;

            case 'completion':
                $completion = $db->query(
                    'SELECT p.email, p.condition_group,
                            MAX(CASE WHEN gd.day = 1 THEN 1 ELSE 0 END) AS day1,
                            MAX(CASE WHEN gd.day = 2 THEN 1 ELSE 0 END) AS day2,
                            MAX(CASE WHEN gd.day = 3 THEN 1 ELSE 0 END) AS day3,
                            COUNT(gd.id) AS total
                     FROM participants p
                     LEFT JOIN game_data gd ON gd.participant_email = p.email
                     GROUP BY p.email, p.condition_group
                     ORDER BY p.email'
                )->fetch_all(MYSQLI_ASSOC);
                break;

            case 'recent':
                $recent = $db->query(
                    'SELECT participant_email, day, condition_group, LENGTH(data) AS size_bytes, submitted_at
                     FROM game_data ORDER BY submitted_at DESC LIMIT 50'
                )->fetch_all(MYSQLI_ASSOC);
                break;

            case 'by_day':
            default:
                $byDay = $db->query(
                    'SELECT day, COUNT(*) AS c FROM game_data GROUP BY day ORDER BY day'
                )->fetch_all(MYSQLI_ASSOC);
                break;
        }

        $db->close();
    } catch (Throwable $e) {
        $error = $e->getMessage();
    }
}

function formatBytes(int $bytes): string
{
    if ($bytes < 1024) return $bytes . ' B';
    return round($bytes / 1024, 1) . ' KB';
}

function viewLink(string $view, string $label, string $current): string
{
    $activeClass = $view === $current ? 'btn' : 'btn secondary';
    return '<a href="?view=' . urlencode($view) . '" class="' . $activeClass . ' btn-small">'
        . htmlspecialchars($label) . '</a>';
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Game Data Admin</title>
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

        input[type="password"] {
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

        input:focus { border-color: #2563eb; }

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

        a.btn {
            display: inline-block;
            text-decoration: none;
        }

        .btn-small { padding: 0.4rem 0.8rem; font-size: 0.65rem; }

        .btn.secondary {
            background: #ffffff;
            color: #2563eb;
            border: 1px solid #bfdbfe;
        }
        .btn.secondary:hover { background: #eff6ff; }

        .error { font-size: 0.8rem; color: #dc2626; padding: 0.75rem; background: #fef2f2; border: 1px solid #fecaca; margin-bottom: 1rem; }

        .layout { display: flex; flex-direction: column; gap: 1.5rem; max-width: 900px; }

        .panel {
            background: #ffffff;
            border: 1px solid #dbe4ee;
            box-shadow: 0 1px 3px rgba(30, 41, 59, 0.06);
            padding: 1.25rem;
        }

        .summary-cards { display: flex; gap: 1rem; flex-wrap: wrap; }
        .summary-card {
            flex: 1;
            min-width: 160px;
            background: #ffffff;
            border: 1px solid #dbe4ee;
            box-shadow: 0 1px 3px rgba(30, 41, 59, 0.06);
            padding: 1rem 1.25rem;
        }
        .summary-card .label {
            font-size: 0.65rem;
            letter-spacing: 0.1em;
            text-transform: uppercase;
            color: #64748b;
            margin-bottom: 0.35rem;
        }
        .summary-card .value { font-size: 1.4rem; font-weight: bold; color: #2563eb; }
        .summary-card .value.small { font-size: 0.95rem; color: #334155; }

        .view-nav { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-bottom: 1rem; }

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

        .check { color: #15803d; font-weight: bold; }
        .cross { color: #cbd5e1; }

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
    </style>
</head>
<body>

<?php if (!$authed): ?>
<div class="login-wrap">
    <div class="card">
        <h1>Game Data <span>Admin</span></h1>
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
    <h1 style="margin:0">Game Data <span style="color:#2563eb">Admin</span></h1>
    <div class="topbar-nav">
        <a href="participant_admin.php" class="btn secondary btn-small">Participants</a>
        <a href="?logout" class="logout">Log out</a>
    </div>
</div>

<div class="layout">
    <?php if ($error): ?>
        <div class="error"><?= htmlspecialchars($error) ?></div>
    <?php endif; ?>

    <div class="summary-cards">
        <div class="summary-card">
            <div class="label">Total entries</div>
            <div class="value"><?= $summary['total'] ?></div>
        </div>
        <div class="summary-card">
            <div class="label">Participants with data</div>
            <div class="value"><?= $summary['participants_with_data'] ?></div>
        </div>
        <div class="summary-card">
            <div class="label">Latest submission</div>
            <div class="value small"><?= $summary['latest'] ? htmlspecialchars($summary['latest']) : 'None yet' ?></div>
        </div>
    </div>

    <div class="panel">
        <h2>Overview</h2>
        <div class="view-nav">
            <?= viewLink('by_day', 'By day', $view) ?>
            <?= viewLink('by_condition', 'By condition', $view) ?>
            <?= viewLink('cross_tab', 'Day x condition', $view) ?>
            <?= viewLink('completion', 'Participant completion', $view) ?>
            <?= viewLink('recent', 'Recent submissions', $view) ?>
        </div>

        <?php if ($view === 'by_day'): ?>
            <?php if (empty($byDay)): ?>
                <div class="empty">No game data yet.</div>
            <?php else: ?>
                <table>
                    <thead><tr><th>Day</th><th>Entries</th></tr></thead>
                    <tbody>
                        <?php foreach ($byDay as $row): ?>
                            <tr>
                                <td>Day <?= (int) $row['day'] ?></td>
                                <td><?= (int) $row['c'] ?></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            <?php endif; ?>

        <?php elseif ($view === 'by_condition'): ?>
            <?php if (empty($byCondition)): ?>
                <div class="empty">No game data yet.</div>
            <?php else: ?>
                <table>
                    <thead><tr><th>Condition</th><th>Entries</th></tr></thead>
                    <tbody>
                        <?php foreach ($byCondition as $row): ?>
                            <tr>
                                <td><?= htmlspecialchars($row['condition_group']) ?></td>
                                <td><?= (int) $row['c'] ?></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            <?php endif; ?>

        <?php elseif ($view === 'cross_tab'): ?>
            <?php if (empty($crossTab)): ?>
                <div class="empty">No game data yet.</div>
            <?php else: ?>
                <table>
                    <thead><tr><th>Day</th><th>Condition</th><th>Entries</th></tr></thead>
                    <tbody>
                        <?php foreach ($crossTab as $row): ?>
                            <tr>
                                <td>Day <?= (int) $row['day'] ?></td>
                                <td><?= htmlspecialchars($row['condition_group']) ?></td>
                                <td><?= (int) $row['c'] ?></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            <?php endif; ?>

        <?php elseif ($view === 'completion'): ?>
            <?php if (empty($completion)): ?>
                <div class="empty">No participants registered yet.</div>
            <?php else: ?>
                <table>
                    <thead>
                        <tr>
                            <th>Email</th>
                            <th>Condition</th>
                            <th>Day 1</th>
                            <th>Day 2</th>
                            <th>Day 3</th>
                            <th>Total submissions</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($completion as $row): ?>
                            <tr>
                                <td><?= htmlspecialchars($row['email']) ?></td>
                                <td><?= htmlspecialchars($row['condition_group']) ?></td>
                                <td class="<?= $row['day1'] ? 'check' : 'cross' ?>"><?= $row['day1'] ? '✓' : '—' ?></td>
                                <td class="<?= $row['day2'] ? 'check' : 'cross' ?>"><?= $row['day2'] ? '✓' : '—' ?></td>
                                <td class="<?= $row['day3'] ? 'check' : 'cross' ?>"><?= $row['day3'] ? '✓' : '—' ?></td>
                                <td><?= (int) $row['total'] ?></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
            <?php endif; ?>

        <?php elseif ($view === 'recent'): ?>
            <?php if (empty($recent)): ?>
                <div class="empty">No game data yet.</div>
            <?php else: ?>
                <table>
                    <thead>
                        <tr>
                            <th>Email</th>
                            <th>Day</th>
                            <th>Condition</th>
                            <th>Size</th>
                            <th>Submitted</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php foreach ($recent as $row): ?>
                            <tr>
                                <td><?= htmlspecialchars($row['participant_email']) ?></td>
                                <td>Day <?= (int) $row['day'] ?></td>
                                <td><?= htmlspecialchars($row['condition_group']) ?></td>
                                <td><?= formatBytes((int) $row['size_bytes']) ?></td>
                                <td><?= htmlspecialchars($row['submitted_at']) ?></td>
                            </tr>
                        <?php endforeach; ?>
                    </tbody>
                </table>
                <p style="font-size:0.7rem;color:#94a3b8;margin-top:0.75rem;">Showing the 50 most recent submissions.</p>
            <?php endif; ?>
        <?php endif; ?>
    </div>
</div>

<?php endif; ?>
</body>
</html>
