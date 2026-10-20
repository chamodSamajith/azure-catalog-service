variable "project_name" {
  description = "Short name used as a prefix for all resource names"
  type        = string
  default     = "catalog"
}

variable "environment" {
  description = "Deployment environment (used in naming and tagging)"
  type        = string
  default     = "tf"
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "australiaeast"
}


variable "sql_admin_login" {
  description = "SQL Server admin username"
  type        = string
  default     = "sqladmin"
}

variable "sql_admin_password" {
  description = "SQL Server admin password (set via TF_VAR_sql_admin_password env var, never committed)"
  type        = string
  sensitive   = true
}

variable "aad_admin_object_id" {
  description = "Object ID of the Entra ID user/group to set as SQL AAD admin"
  type        = string
  default     = "340c8e25-4b46-418b-9837-d3064ce709f7"
}

variable "aad_admin_login" {
  description = "Display name of the Entra ID admin for SQL"
  type        = string
  default     = "chamodsr"
}

variable "my_ip" {
  description = "Your IP address, for the SQL firewall allow rule"
  type        = string
}