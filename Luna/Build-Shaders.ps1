Set-Location $(Split-Path $MyInvocation.MyCommand.Path) -ErrorAction Stop
Set-Location Resources -ErrorAction Stop

Get-ChildItem -Filter '*.hlsl' | % {
    $OutputFile = [System.IO.Path]::ChangeExtension($_.Name, ".dxbc")
    $OutputItem = Get-Item $OutputFile -ErrorAction SilentlyContinue
    if (($OutputItem -eq $null) -or ($OutputItem.LastWriteTime -lt $_.LastWriteTime)) {
        $NameParts = [System.IO.Path]::GetFileNameWithoutExtension($_.Name).Split('_')
        $ProgramType = $NameParts[$NameParts.Length - 1]
        & fxc /nologo /T ${ProgramType}_5_0 /O3 /Fo $OutputFile $_.Name
    }
}
