git diff --name-only | ForEach-Object { 
    $dest = (Join-Path ".\diff" $_) + ".diff";
    New-Item -ItemType File -Path $dest -Force; 
    git diff $_ | Out-File -FilePath $dest -Encoding utf8 
}
