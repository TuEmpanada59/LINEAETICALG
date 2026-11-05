# Genera una llave AES-256 en base64 para Cifrado:Llave.
# Guárdela en user-secrets o en Key Vault, nunca en el repositorio. Si se pierde, lo cifrado con ella no se puede leer.
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
