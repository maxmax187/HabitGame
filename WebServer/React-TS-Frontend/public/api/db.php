<?php
require_once __DIR__ . '/env.php';
loadEnv();

// All 4 main conditions now run the full 3 days. BETWEEN_* participants
// are tested only at the end of day 3; WITHIN_* participants are tested
// on both day 1 and day 3 (both handled entirely in-game) - each crossed
// with L/R bias. Participants are balanced equally across these four.
const BALANCED_CONDITIONS = ['BETWEEN_L', 'BETWEEN_R', 'WITHIN_L', 'WITHIN_R'];

// SHORT is a fifth, separate condition - a single simplified session for
// participants who registered but don't want the full 3-day study.
// Deliberately excluded from BALANCED_CONDITIONS: never auto-assigned and
// never touched by "reassign all", only reachable via a forced assignment.
const CONDITIONS = [...BALANCED_CONDITIONS, 'SHORT'];

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

// Only ever picks among BALANCED_CONDITIONS - SHORT is never auto-assigned,
// even though $counts (from countByCondition) also includes its count.
function pickBalancedCondition(array $counts): string {
    $balanced = array_intersect_key($counts, array_flip(BALANCED_CONDITIONS));
    $minCount = min($balanced);
    $candidates = array_keys($balanced, $minCount, true);
    return $candidates[array_rand($candidates)];
}

// SHORT is a single session; every other condition now runs the full 3 days.
function conditionDayCount(string $condition): int {
    return $condition === 'SHORT' ? 1 : 3;
}
