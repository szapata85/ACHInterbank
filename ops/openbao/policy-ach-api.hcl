path "secret/data/certificates/*" {
  capabilities = ["create", "update", "read", "delete", "list"]
}

path "secret/metadata/certificates/*" {
  capabilities = ["read", "list", "delete"]
}
