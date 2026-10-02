$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$files = @(
    'libs\server\Custom\CustomRespCommands.cs',
    'libs\server\Objects\Hash\HashObject.cs',
    'libs\server\Objects\SortedSet\SortedSetObject.cs',
    'libs\server\Resp\BasicCommands.cs',
    'libs\server\Resp\BasicEtagCommands.cs',
    'libs\server\Resp\KeyAdminCommands.cs',
    'libs\server\Storage\Functions\LogRecordUtils.cs',
    'libs\server\Storage\Session\MainStore\MainStoreOps.cs'
)
foreach ($relative in $files) {
    $path = Join-Path $root $relative
    $content = [System.IO.File]::ReadAllText($path)
    $updated = $content.Replace('DateTimeOffset.UtcNow.Ticks', 'Garnet.server.GarnetExecutionTime.UtcTicks')
    if ($updated -ne $content) {
        [System.IO.File]::WriteAllText($path, $updated, [System.Text.UTF8Encoding]::new($true))
    }
}
