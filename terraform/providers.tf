terraform {
  required_version = ">= 1.9"

    backend "azurerm" {
    resource_group_name  = "catalog-rg-tf-state"
    storage_account_name = "sttfstatecatalog"
    container_name       = "tfstate"
    key                  = "catalog.terraform.tfstate"
  }

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
  }
}


provider "azurerm" {
  features {}
}

provider "azuread" {}