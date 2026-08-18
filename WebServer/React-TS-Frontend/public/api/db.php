<?php
require_once __DIR__ . '/env.php';
loadEnv();

function getDb(): mysqli {
    $db = @new mysqli(
        getenv('DB_HOST') ?: 'localhost',
        getenv('DB_USER'),
        getenv('DB_PASSWORD'),
        getenv('DB_NAME'),
        (int) (getenv('DB_PORT') ?: 3306)
    );

    if ($db->connect_error) {
        throw new RuntimeException('Database connection failed: ' . $db->connect_error);
    }

    $db->set_charset('utf8mb4');
    return $db;
}

function countByCondition(mysqli $db): array {
    $counts = ['L' => 0, 'R' => 0];
    $result = $db->query('SELECT condition_group, COUNT(*) AS c FROM participants GROUP BY condition_group');
    while ($row = $result->fetch_assoc()) {
        $counts[$row['condition_group']] = (int) $row['c'];
    }
    return $counts;
}

function pickBalancedCondition(array $counts): string {
    if ($counts['L'] < $counts['R']) return 'L';
    if ($counts['R'] < $counts['L']) return 'R';
    return random_int(0, 1) === 0 ? 'L' : 'R';
}
