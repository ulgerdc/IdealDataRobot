

$sourceDirectory = "C:\iko\robot\denali"
$outputFile = "C:\iko\CombinedClasses.cs"
$commonNamespace = "ideal"

Get-ChildItem -Path $sourceDirectory -Filter *.cs | ForEach-Object {
    $content = Get-Content $_.FullName | Out-String
    $classRegex = '(?ms)(class.*?{.*?})'
    $matches = [Regex]::Matches($content, $classRegex)

    $combinedContent = foreach ($match in $matches) {
        $match.Groups[1].Value
    }

    if ($combinedContent) {
        $combinedContent = "namespace $commonNamespace {" + $combinedContent + "}"
        $combinedContent | Out-File -Append -FilePath $outputFile
    }
}
