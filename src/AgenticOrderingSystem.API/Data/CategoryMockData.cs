using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Data;

/// <summary>
/// Mock data generator for Categories
/// </summary>
public static class CategoryMockData
{
    public static List<Category> GetMockCategories()
    {
        return new List<Category>
        {
            new Category
            {
                Id = "cat_software",
                Name = "Software & Licenses",
                Description = "Software applications, licenses, and digital tools",
                IsActive = true,
                SortOrder = 1,
                Metadata = new CategoryMetadata
                {
                    Icon = "💻",
                    Color = "#2196F3",
                    DefaultApprovalModel = new ApprovalModel
                    {
                        Level1 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "manager",
                            TimeoutHours = 48,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 100,
                                MaxAmount = 10000
                            }
                        },
                        Level2 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "director",
                            TimeoutHours = 72,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 5000
                            }
                        }
                    },
                    RequiredFields = new List<string> { "businessJustification", "estimatedCost", "vendor" },
                    BusinessRules = new List<string> { "Must provide security assessment for external software" }
                }
            },
            new Category
            {
                Id = "cat_hardware",
                Name = "Hardware & Equipment",
                Description = "Computer hardware, peripherals, and physical equipment",
                IsActive = true,
                SortOrder = 2,
                Metadata = new CategoryMetadata
                {
                    Icon = "🖥️",
                    Color = "#FF5722",
                    DefaultApprovalModel = new ApprovalModel
                    {
                        Level1 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "manager",
                            TimeoutHours = 48,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 500,
                                MaxAmount = 15000
                            }
                        },
                        Level2 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "director",
                            TimeoutHours = 72,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 3000
                            }
                        }
                    },
                    RequiredFields = new List<string> { "specifications", "quantity", "vendor", "warrantyInfo" },
                    BusinessRules = new List<string> { "Hardware must meet company security standards" }
                }
            },
            new Category
            {
                Id = "cat_training",
                Name = "Training & Development",
                Description = "Professional development, courses, certifications, and training programs",
                IsActive = true,
                SortOrder = 3,
                Metadata = new CategoryMetadata
                {
                    Icon = "📚",
                    Color = "#4CAF50",
                    DefaultApprovalModel = new ApprovalModel
                    {
                        Level1 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "manager",
                            TimeoutHours = 24,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 200,
                                MaxAmount = 5000
                            }
                        }
                    },
                    RequiredFields = new List<string> { "courseDetails", "businessRelevance", "provider" },
                    BusinessRules = new List<string> { "Training must align with career development goals" }
                }
            },
            new Category
            {
                Id = "cat_services",
                Name = "Professional Services",
                Description = "Consulting, support services, and professional expertise",
                IsActive = true,
                SortOrder = 4,
                Metadata = new CategoryMetadata
                {
                    Icon = "🤝",
                    Color = "#9C27B0",
                    DefaultApprovalModel = new ApprovalModel
                    {
                        Level1 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "manager",
                            TimeoutHours = 48,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 1000,
                                MaxAmount = 25000
                            }
                        },
                        Level2 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "md",
                            TimeoutHours = 96,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 10000
                            }
                        }
                    },
                    RequiredFields = new List<string> { "serviceDescription", "deliverables", "timeline", "vendor" },
                    BusinessRules = new List<string> { "Requires detailed statement of work", "Must have clear deliverables" }
                }
            },
            new Category
            {
                Id = "cat_travel",
                Name = "Travel & Expenses",
                Description = "Business travel, conferences, and related expenses",
                IsActive = true,
                SortOrder = 5,
                Metadata = new CategoryMetadata
                {
                    Icon = "✈️",
                    Color = "#FF9800",
                    DefaultApprovalModel = new ApprovalModel
                    {
                        Level1 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "manager",
                            TimeoutHours = 24,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 500,
                                MaxAmount = 8000
                            }
                        },
                        Level2 = new ApprovalLevel
                        {
                            Required = true,
                            ApproverType = "director",
                            TimeoutHours = 48,
                            TriggerConditions = new TriggerConditions
                            {
                                MinAmount = 3000
                            }
                        }
                    },
                    RequiredFields = new List<string> { "destination", "businessPurpose", "dates", "estimatedCost" },
                    BusinessRules = new List<string> { "Must follow company travel policy", "International travel requires additional approval" }
                }
            }
        };
    }
}
