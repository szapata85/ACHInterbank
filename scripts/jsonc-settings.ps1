# Auxiliar compatible con Windows PowerShell 5.1 y PowerShell 7.
# El appsettings.json usa unicamente comentarios // en lineas independientes.
function Read-JsoncSettings {
    param([Parameter(Mandatory=$true)][string]$Path)
    $source = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $withoutComments = [regex]::Replace($source, '(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)', '')
    return ConvertFrom-Json -InputObject $withoutComments
}

function Set-JsoncSetting {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Section,
        [Parameter(Mandatory=$true)][string]$Name,
        [Parameter(Mandatory=$true)]$Value
    )
    $source = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $blockRegex = '"' + [regex]::Escape($Section) + '"\s*:\s*\{(?<body>[^{}]*)\}'
    $block = [regex]::Match($source, $blockRegex, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $block.Success) { throw "No se encontro seccion plana '$Section' en $Path" }
    $body = $block.Groups['body']
    $settingRegex = '(?m)^([ \t]*"' + [regex]::Escape($Name) + '"[ \t]*:[ \t]*)(?:"(?:\\.|[^"\\])*"|true|false|null|-?\d+(?:\.\d+)?)'
    $match = [regex]::Match($body.Value, $settingRegex)
    if (-not $match.Success) { throw "No se encontro parametro '${Section}:$Name' en $Path" }
    $jsonValue = if ($null -eq $Value) { 'null' } elseif ($Value -is [bool]) { $Value.ToString().ToLowerInvariant() } else { ConvertTo-Json -InputObject $Value -Compress }
    $start = $body.Index + $match.Index + $match.Groups[1].Length
    $oldLength = $match.Length - $match.Groups[1].Length
    $updated = $source.Substring(0, $start) + $jsonValue + $source.Substring($start + $oldLength)
    # Comprobar JSON antes de guardar, sin deshacerse de sus comentarios.
    $withoutComments = [regex]::Replace($updated, '(?m)^[ \t]*//[^\r\n]*(?:\r?\n|$)', '')
    [void](ConvertFrom-Json -InputObject $withoutComments)
    [System.IO.File]::WriteAllText($Path, $updated, (New-Object System.Text.UTF8Encoding($false)))
}
