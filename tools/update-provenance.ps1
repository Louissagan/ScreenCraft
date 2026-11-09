$ErrorActionPreference='Stop'
# The 0.9.4 upstream manifest is historical and must not be overwritten.
& (Join-Path $PSScriptRoot 'audit-core.ps1') @args
