resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-rg-${var.environment}"
  location = var.location

  tags = {
    project     = var.project_name
    environment = var.environment
    managed_by  = "terraform"
  }
}

resource "azurerm_mssql_server" "main" {
  name                         = "sql-${var.project_name}-${var.environment}"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_login
  administrator_login_password = var.sql_admin_password
  minimum_tls_version          = "1.2"

  azuread_administrator {
    login_username = var.aad_admin_login
    object_id      = var.aad_admin_object_id
  }

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_mssql_database" "main" {
  name                        = "CatalogDb"
  server_id                   = azurerm_mssql_server.main.id
  sku_name                    = "GP_S_Gen5_1"
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  storage_account_type         = "Local"
  # use_free_limit / free_limit_exhaustion_behavior are not yet exposed by the
  # azurerm provider (v4.81.0). Applied manually after `terraform apply` via:
  #   az sql db update --resource-group <rg> --server <server> --name CatalogDb \
  #     --use-free-limit --free-limit-exhaustion-behavior AutoPause
  # This is a documented gap, not an oversight — terraform plan will not show
  # drift for this setting because it isn't tracked in state at all.

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_mssql_firewall_rule" "allow_my_ip" {
  name             = "AllowMyIP"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = var.my_ip
  end_ip_address   = var.my_ip
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
