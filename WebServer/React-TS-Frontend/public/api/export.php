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
// Release the session lock so the dashboards stay usable during a long export.
session_write_close();

if (!class_exists('ZipArchive')) {
    http_response_code(500);
    exit('The PHP "zip" extension is not enabled on this server, so a .zip export cannot be built. Ask whoever manages the hosting to enable ext-zip.');
}

@set_time_limit(0);

$tmpZipPath = null;

// A fatal error (e.g. memory or time limit) otherwise shows up as a blank 500.
register_shutdown_function(function () use (&$tmpZipPath) {
    $err = error_get_last();
    if ($err !== null && in_array($err['type'], [E_ERROR, E_CORE_ERROR, E_COMPILE_ERROR], true)) {
        // PHP has already set the 500 status for the fatal error.
        if (!headers_sent()) {
            header('Content-Type: text/plain');
        }
        echo 'Export failed: ' . $err['message'] . ' (details in api/php-errors.log)';
    }
    if ($tmpZipPath !== null && is_file($tmpZipPath)) {
        @unlink($tmpZipPath);
    }
});

function failExport(string $message): void {
    error_log('[export] ' . $message);
    http_response_code(500);
    header('Content-Type: text/plain');
    exit('Export failed: ' . $message);
}

$tmpZipPath = tempnam(sys_get_temp_dir(), 'gamedata_');
if ($tmpZipPath === false) {
    $tmpZipPath = null;
    failExport('could not create a temporary file in ' . sys_get_temp_dir() . '.');
}

try {
    $db = getDb();
} catch (Throwable $e) {
    failExport('database connection failed: ' . $e->getMessage());
}

// Unbuffered, so only one row's data is in memory at a time.
$result = $db->query(
    'SELECT id, participant_email, day, condition_group, data, submitted_at
     FROM game_data ORDER BY participant_email, day, submitted_at',
    MYSQLI_USE_RESULT
);
if ($result === false) {
    failExport('database query failed: ' . $db->error);
}

// ZipArchive keeps every added string in memory for the lifetime of the
// object (even across close/open), so write the archive out in batches,
// each with a fresh object.
const EXPORT_BATCH_SIZE = 25;

$zip = new ZipArchive();
if ($zip->open($tmpZipPath, ZipArchive::OVERWRITE) !== true) {
    failExport('could not create zip archive.');
}

$count = 0;
while ($row = $result->fetch_assoc()) {
    $safeEmail = preg_replace('/[^a-zA-Z0-9]+/', '_', $row['participant_email']);
    $safeTimestamp = preg_replace('/[^0-9]+/', '-', $row['submitted_at']);
    // id is included to guarantee a unique filename per row even if two
    // submissions from the same participant/day land in the same second.
    $name = "{$row['condition_group']}_day{$row['day']}_{$safeEmail}_{$safeTimestamp}_id{$row['id']}.json";
    if (!$zip->addFromString($name, $row['data'])) {
        failExport("could not add $name to the zip archive.");
    }
    unset($row);
    $count++;

    if ($count % EXPORT_BATCH_SIZE === 0) {
        if (!$zip->close()) {
            failExport("could not write the zip archive after $count entries.");
        }
        $zip = new ZipArchive();
        if ($zip->open($tmpZipPath) !== true) {
            failExport("could not write the zip archive after $count entries.");
        }
    }
}
$result->free();
$db->close();

if ($count === 0) {
    // An empty ZipArchive is never written to disk.
    $zip->addFromString('README.txt', 'No game data entries yet.');
}
if (!$zip->close()) {
    failExport('could not finalise the zip archive.');
}

$downloadName = 'game_data_export_' . date('Y-m-d_His') . '.zip';

// Stream the file rather than letting an output buffer hold all of it.
while (ob_get_level() > 0) {
    ob_end_clean();
}
header('Content-Type: application/zip');
header('Content-Disposition: attachment; filename="' . $downloadName . '"');
header('Content-Length: ' . filesize($tmpZipPath));
readfile($tmpZipPath);
exit;
