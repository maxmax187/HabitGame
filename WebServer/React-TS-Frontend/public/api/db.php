<?php
require_once __DIR__ . '/env.php';
loadEnv();

// ML/MR = moderate dosage (1 day), EL/ER = extensive dosage (3 days),
// each crossed with L/R bias. Participants are balanced equally across
// all four.
const CONDITIONS = ['ML', 'MR', 'EL', 'ER'];

function getDb(): mysqli {
    // PHP 8.1+ makes mysqli throw on SQL errors (e.g. duplicate key) by
    // default. This code expects the classic behavior - check
    // $db->errno / $stmt->execute() return value - so turn that off.
    mysqli_report(MYSQLI_REPORT_OFF);

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
    $counts = array_fill_keys(CONDITIONS, 0);
    $result = $db->query('SELECT condition_group, COUNT(*) AS c FROM participants GROUP BY condition_group');
    while ($row = $result->fetch_assoc()) {
        $counts[$row['condition_group']] = (int) $row['c'];
    }
    return $counts;
}

function pickBalancedCondition(array $counts): string {
    $minCount = min($counts);
    $candidates = array_keys($counts, $minCount, true);
    return $candidates[array_rand($candidates)];
}

// ML/MR (moderate) only ever have a single day; EL/ER (extensive) have 3.
function conditionDayCount(string $condition): int {
    return in_array($condition, ['ML', 'MR'], true) ? 1 : 3;
}
