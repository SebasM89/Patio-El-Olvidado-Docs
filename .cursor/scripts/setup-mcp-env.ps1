# Setup MCP — Patio El Olvidado (PowerShell)

# 1) Editá los valores y ejecutá UNA vez (variables de usuario de Windows).
# 2) Cerrá y reabrí Cursor.
# 3) Settings → MCP: github y mssql en verde.

param(
  [string]$GithubToken = "",
  [string]$SqlServer = "localhost",
  [string]$SqlPort = "1433",
  [string]$SqlDatabase = "ElOlvidado",
  [string]$SqlUser = "sa",
  [string]$SqlPassword = "",
  [string]$SqlEncrypt = "false",
  [string]$SqlTrustCert = "true"
)

if (-not $GithubToken) {
  Write-Error "Pasá -GithubToken 'ghp_...'"
  exit 1
}
if (-not $SqlPassword) {
  Write-Error "Pasá -SqlPassword 'tu_password'"
  exit 1
}

$vars = @{
  GITHUB_PERSONAL_ACCESS_TOKEN = $GithubToken
  MSSQL_SERVER = $SqlServer
  MSSQL_PORT = $SqlPort
  MSSQL_DATABASE = $SqlDatabase
  MSSQL_USER = $SqlUser
  MSSQL_PASSWORD = $SqlPassword
  MSSQL_ENCRYPT = $SqlEncrypt
  MSSQL_TRUST_SERVER_CERTIFICATE = $SqlTrustCert
}

foreach ($k in $vars.Keys) {
  [System.Environment]::SetEnvironmentVariable($k, $vars[$k], "User")
  Write-Host "OK $k"
}

Write-Host ""
Write-Host "Listo. Reiniciá Cursor y verificá MCP (github + mssql)."
