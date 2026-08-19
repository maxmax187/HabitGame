<?php
ini_set('display_errors', 0);
ini_set('log_errors', 1);
ini_set('error_log', __DIR__ . '/php-errors.log');
error_reporting(E_ALL);

require_once __DIR__ . '/env.php';
loadEnv();
require_once __DIR__ . '/db.php';

session_start();

$authed = isset($_SESSION['admin_auth']) && $_SESSION['admin_auth'] === true;
if (!$authed) {
    http_response_code(403);
    exit('Forbidden - log in via data_admin.php or participant_admin.php first.');
}

if (!class_exists('ZipArchive')) {
    http_response_code(500);
    exit('The PHP "zip" extension is not enabled on this server, so a .zip export cannot be built. Ask whoever manages the hosting to enable ext-zip.');
}

try {
    $db = getDb();
} catch (Throwable $e) {
    http_response_code(500);
    exit('Database connection failed: ' . $e->getMessage());
}

$rows = $db->query(
    'SELECT id, participant_email, day, condition_group, data, submitted_at
     FROM game_data ORDER BY participant_email, day, submitted_at'
)->fetch_all(MYSQLI_ASSOC);
$db->close();

$tmpZipPath = tempnam(sys_get_temp_dir(), 'gamedata_');
$zip = new ZipArchive();
if ($zip->open($tmpZipPath, ZipArchive::OVERWRITE) !== true) {
    http_response_code(500);
    exit('Could not create zip archive.');
}

foreach ($rows as $row) {
    $safeEmail = preg_replace('/[^a-zA-Z0-9]+/', '_', $row['participant_email']);
    $safeTimestamp = preg_replace('/[^0-9]+/', '-', $row['submitted_at']);
    // id is included to guarantee a unique filename per row even if two
    // submissions from the same participant/day land in the same second.
    $name = "{$row['condition_group']}_day{$row['day']}_{$safeEmail}_{$safeTimestamp}_id{$row['id']}.json";
    $zip->addFromString($name, $row['data']);
}

$zip->close();

$downloadName = 'game_data_export_' . date('Y-m-d_His') . '.zip';

header('Content-Type: application/zip');
header('Content-Disposition: attachment; filename="' . $downloadName . '"');
header('Content-Length: ' . filesize($tmpZipPath));
readfile($tmpZipPath);
unlink($tmpZipPath);
exit;
