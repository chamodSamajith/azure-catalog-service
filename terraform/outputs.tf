output "web_app_url" {
  description = "Default hostname of the deployed web app"
  value       = azurerm_linux_web_app.main.default_hostname
}

output "web_app_principal_id" {
  description = "Managed identity principal ID — needed for the SQL contained user step"
  value       = azurerm_linux_web_app.main.identity[0].principal_id
}
