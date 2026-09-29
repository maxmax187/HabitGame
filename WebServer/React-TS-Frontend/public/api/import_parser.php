<?php
// Helpers for the bulk email import in participant_admin.php.
// Reads the first sheet of an .xlsx (e.g. a Microsoft Forms export) or a
// .csv/.txt file into a list of rows (each a list of cell strings).

const IMPORT_MAX_BYTES = 5 * 1024 * 1024;
const XLSX_NS = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main';

function readSpreadsheet(string $path, string $originalName): array {
    $ext = strtolower(pathinfo($originalName, PATHINFO_EXTENSION));
    if ($ext === 'xlsx') {
        return readXlsx($path);
    }
    if ($ext === 'csv' || $ext === 'txt') {
        return readCsv($path);
    }
    throw new RuntimeException('Unsupported file type ".' . $ext . '". Upload an .xlsx, .csv or .txt file.');
}

function readCsv(string $path): array {
    $text = file_get_contents($path);
    if ($text === false) {
        throw new RuntimeException('Could not read the uploaded file.');
    }
    $text = preg_replace('/^\xEF\xBB\xBF/', '', $text);
    if (function_exists('mb_convert_encoding') && !preg_match('//u', $text)) {
        $text = mb_convert_encoding($text, 'UTF-8', 'Windows-1252');
    }

    // Excel in a Dutch locale writes ';'-separated CSV, so sniff the delimiter.
    $firstLine = strtok($text, "\r\n") ?: '';
    $delimiter = ',';
    $best = 0;
    foreach ([',', ';', "\t"] as $d) {
        if (substr_count($firstLine, $d) > $best) {
            $best = substr_count($firstLine, $d);
            $delimiter = $d;
        }
    }

    $rows = [];
    $fh = fopen('php://memory', 'r+');
    fwrite($fh, $text);
    rewind($fh);
    while (($row = fgetcsv($fh, 0, $delimiter, '"', '')) !== false) {
        if ($row === [null]) {
            continue;
        }
        $rows[] = array_map(fn($v) => (string) $v, $row);
    }
    fclose($fh);
    return $rows;
}

function readXlsx(string $path): array {
    if (!class_exists('ZipArchive')) {
        throw new RuntimeException('The PHP "zip" extension is not enabled on this server, so .xlsx files cannot be read. Save the file as .csv in Excel and upload that instead.');
    }
    $zip = new ZipArchive();
    if ($zip->open($path) !== true) {
        throw new RuntimeException('Could not open the .xlsx file. Is it a valid Excel workbook?');
    }

    try {
        $sharedStrings = [];
        $ssXml = zipEntry($zip, 'xl/sharedStrings.xml', false);
        if ($ssXml !== null) {
            $doc = loadXml($ssXml);
            foreach ($doc->getElementsByTagNameNS(XLSX_NS, 'si') as $si) {
                // Rich text splits a string into several <t> runs.
                $s = '';
                foreach ($si->getElementsByTagNameNS(XLSX_NS, 't') as $t) {
                    $s .= $t->textContent;
                }
                $sharedStrings[] = $s;
            }
        }

        $sheet = loadXml(zipEntry($zip, firstSheetPath($zip), true));
    } finally {
        $zip->close();
    }

    $rows = [];
    foreach ($sheet->getElementsByTagNameNS(XLSX_NS, 'row') as $rowEl) {
        $row = [];
        foreach ($rowEl->getElementsByTagNameNS(XLSX_NS, 'c') as $c) {
            // Empty cells are omitted from the XML, so place each by its reference.
            $col = columnIndex($c->getAttribute('r'));
            if ($col === null) {
                $col = count($row);
            }
            $type = $c->getAttribute('t');
            $v = $c->getElementsByTagNameNS(XLSX_NS, 'v')->item(0);
            if ($type === 'inlineStr') {
                $value = $c->textContent;
            } elseif ($v === null) {
                $value = '';
            } elseif ($type === 's') {
                $value = $sharedStrings[(int) $v->textContent] ?? '';
            } else {
                $value = $v->textContent;
            }
            $row[$col] = $value;
        }
        if ($row) {
            $filled = array_fill(0, max(array_keys($row)) + 1, '');
            $rows[] = array_replace($filled, $row);
        }
    }
    return $rows;
}

function firstSheetPath(ZipArchive $zip): string {
    $fallback = 'xl/worksheets/sheet1.xml';
    $wbXml = zipEntry($zip, 'xl/workbook.xml', false);
    $relsXml = zipEntry($zip, 'xl/_rels/workbook.xml.rels', false);
    if ($wbXml === null || $relsXml === null) {
        return $fallback;
    }
    $sheetEl = loadXml($wbXml)->getElementsByTagNameNS(XLSX_NS, 'sheet')->item(0);
    if ($sheetEl === null) {
        return $fallback;
    }
    $rId = $sheetEl->getAttributeNS('http://schemas.openxmlformats.org/officeDocument/2006/relationships', 'id');
    foreach (loadXml($relsXml)->getElementsByTagName('Relationship') as $rel) {
        if ($rel->getAttribute('Id') === $rId) {
            $target = $rel->getAttribute('Target');
            return str_starts_with($target, '/') ? ltrim($target, '/') : 'xl/' . $target;
        }
    }
    return $fallback;
}

function zipEntry(ZipArchive $zip, string $name, bool $required): ?string {
    $stat = $zip->statName($name);
    if ($stat === false) {
        if ($required) {
            throw new RuntimeException("The .xlsx file is missing $name.");
        }
        return null;
    }
    // Guard against zip bombs; a real registration export is tiny.
    if ($stat['size'] > 50 * 1024 * 1024) {
        throw new RuntimeException('The .xlsx file is too large to import.');
    }
    $data = $zip->getFromName($name);
    if ($data === false) {
        throw new RuntimeException("Could not read $name from the .xlsx file.");
    }
    return $data;
}

function loadXml(string $xml): DOMDocument {
    $doc = new DOMDocument();
    if (!@$doc->loadXML($xml, LIBXML_NONET)) {
        throw new RuntimeException('The .xlsx file contains invalid XML.');
    }
    return $doc;
}

function columnIndex(string $cellRef): ?int {
    if (!preg_match('/^([A-Z]+)\d+$/', $cellRef, $m)) {
        return null;
    }
    $n = 0;
    foreach (str_split($m[1]) as $ch) {
        $n = $n * 26 + (ord($ch) - 64);
    }
    return $n - 1;
}

function normalizeEmail(string $value): string {
    // Forms/Excel can leave non-breaking or zero-width spaces around values.
    $value = preg_replace('/^[\s\x{00A0}\x{200B}\x{FEFF}]+|[\s\x{00A0}\x{200B}\x{FEFF}]+$/u', '', $value) ?? $value;
    return strtolower($value);
}

function isValidEmail(string $value): bool {
    return filter_var($value, FILTER_VALIDATE_EMAIL) !== false;
}

// Column labels for the preview. Row 1 is treated as the header unless it
// has no text at all (e.g. a headerless list of emails).
function importColumns(array $rows): array {
    $width = 0;
    foreach ($rows as $r) {
        $width = max($width, count($r));
    }
    $header = $rows[0] ?? [];
    $columns = [];
    for ($i = 0; $i < $width; $i++) {
        $label = trim((string) ($header[$i] ?? ''));
        $count = 0;
        foreach ($rows as $r) {
            if (isValidEmail(normalizeEmail((string) ($r[$i] ?? '')))) {
                $count++;
            }
        }
        $columns[] = [
            'label' => $label !== '' ? $label : 'Column ' . ($i + 1),
            'emails' => $count,
        ];
    }
    return $columns;
}

// The column name in the form export isn't fixed, and Forms adds its own
// "Email" column (usually "anonymous"), so pick the column by content.
// Ties go to the later column: Forms puts question answers after its own
// metadata columns.
function detectEmailColumn(array $columns): ?int {
    $best = null;
    foreach ($columns as $i => $col) {
        if ($col['emails'] > 0 && ($best === null || $col['emails'] >= $columns[$best]['emails'])) {
            $best = $i;
        }
    }
    return $best;
}

// One entry per non-empty cell in the chosen column. $registered is a set
// (email => true) of emails already in the database.
function buildImportEntries(array $rows, int $col, array $registered): array {
    $entries = [];
    $seen = [];
    foreach ($rows as $r => $row) {
        $raw = trim((string) ($row[$col] ?? ''));
        $email = normalizeEmail($raw);
        if ($email === '') {
            continue;
        }
        $valid = isValidEmail($email);
        if ($r === 0 && !$valid) {
            continue; // header row
        }
        if (!$valid) {
            $status = 'invalid';
        } elseif (isset($seen[$email])) {
            $status = 'duplicate';
        } elseif (isset($registered[$email])) {
            $status = 'registered';
        } else {
            $status = 'new';
        }
        $seen[$email] = true;
        $entries[] = ['row' => $r + 1, 'email' => $valid ? $email : $raw, 'status' => $status];
    }
    return $entries;
}
