<?php
ini_set('display_errors', 0);
ini_set('log_errors', 1);
ini_set('error_log', __DIR__ . '/php-errors.log');
error_reporting(E_ALL);

require_once __DIR__ . '/db.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Method not allowed']);
    exit;
}

$body = json_decode(file_get_contents('php://input'), true);

$email = isset($body['email']) ? strtolower(trim($body['email'])) : '';
$day = isset($body['day']) ? (int) $body['day'] : 0;
$data = $body['data'] ?? null;

if (!filter_var($email, FILTER_VALIDATE_EMAIL) || $day < 1 || $day > 3 || $data === null) {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'Missing or invalid email, day, or data']);
    exit;
}

try {
    $db = getDb();
} catch (Throwable $e) {
    error_log($e->getMessage());
    http_response_code(500);
    echo json_encode(['success' => false, 'message' => 'Server error']);
    exit;
}

$stmt = $db->prepare('SELECT condition_group FROM participants WHERE email = ?');
$stmt->bind_param('s', $email);
$stmt->execute();
$participant = $stmt->get_result()->fetch_assoc();

if (!$participant) {
    $db->close();
    http_response_code(404);
    echo json_encode(['success' => false, 'message' => 'Unknown participant']);
    exit;
}

// Round-trip through json_decode/json_encode so we never store whatever
// arbitrary bytes were POSTed - only well-formed JSON.
$dataJson = json_encode($data);

$stmt = $db->prepare(
    'INSERT INTO game_data (participant_email, day, condition_group, data) VALUES (?, ?, ?, ?)'
);
$stmt->bind_param('siss', $email, $day, $participant['condition_group'], $dataJson);

if ($stmt->execute()) {
    echo json_encode(['success' => true]);
} else {
    http_response_code(500);
    echo json_encode(['success' => false, 'message' => 'Failed to save data']);
}

$db->close();
