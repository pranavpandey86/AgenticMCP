using AgenticOrderingSystem.API.Models;

namespace AgenticOrderingSystem.API.Data;

/// <summary>
/// Mock data generator for Products with realistic business scenarios
/// </summary>
public static class ProductMockData
{
    public static List<Product> GetMockProducts()
    {
        return new List<Product>
        {
            // SOFTWARE PRODUCTS
            new Product
            {
                Id = "prod_adobe_creative",
                Name = "Adobe Creative Suite License",
                Description = "Complete Adobe Creative Cloud suite including Photoshop, Illustrator, InDesign, Premiere Pro",
                Category = "software",
                Price = 600,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
                {
                    Level1 = new ApprovalLevel
                    {
                        Required = true,
                        ApproverType = "manager",
                        TimeoutHours = 48,
                        TriggerConditions = new TriggerConditions
                        {
                            MinAmount = 600,
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
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_software_name",
                        Key = "softwareName",
                        Question = "What specific Adobe software do you need?",
                        Type = "multiselect",
                        Required = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "photoshop", Label = "Photoshop" },
                            new QuestionOption { Value = "illustrator", Label = "Illustrator" },
                            new QuestionOption { Value = "indesign", Label = "InDesign" },
                            new QuestionOption { Value = "premiere", Label = "Premiere Pro" },
                            new QuestionOption { Value = "aftereffects", Label = "After Effects" },
                            new QuestionOption { Value = "complete", Label = "Complete Creative Cloud" }
                        },
                        HelpText = "Select all software applications you need access to"
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_business_justification",
                        Key = "businessJustification",
                        Question = "Provide detailed business justification for this software",
                        Type = "textarea",
                        Required = true,
                        Order = 2,
                        Validation = new QuestionValidation
                        {
                            Length = new LengthValidation { Min = 50, Max = 500 }
                        },
                        HelpText = "Explain how this software will be used for business purposes and expected benefits"
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_estimated_cost",
                        Key = "estimatedCost",
                        Question = "Estimated annual cost (USD)",
                        Type = "number",
                        Required = true,
                        Order = 3,
                        Validation = new QuestionValidation
                        {
                            Min = 100,
                            Max = 10000
                        },
                        HelpText = "Include subscription fees, training costs, and any additional expenses"
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_urgency",
                        Key = "urgency",
                        Question = "How urgent is this request?",
                        Type = "select",
                        Required = true,
                        Order = 4,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "low", Label = "Low - Can wait 2-4 weeks" },
                            new QuestionOption { Value = "medium", Label = "Medium - Needed within 2 weeks" },
                            new QuestionOption { Value = "high", Label = "High - Needed within 1 week" },
                            new QuestionOption { Value = "urgent", Label = "Urgent - Needed immediately" }
                        }
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "Adobe Inc.",
                    BusinessUnit = "Design & Marketing",
                    Tags = new List<string> { "design", "creative", "subscription", "professional" },
                    EstimatedCost = 600,
                    Currency = "USD"
                }
            },

            new Product
            {
                Id = "prod_jira_confluence",
                Name = "Atlassian Jira & Confluence License",
                Description = "Project management and collaboration tools for development teams",
                Category = "software",
                Price = 1200,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
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
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_team_size",
                        Key = "teamSize",
                        Question = "How many team members need access?",
                        Type = "number",
                        Required = true,
                        Order = 1,
                        Validation = new QuestionValidation { Min = 1, Max = 100 }
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_project_type",
                        Key = "projectType",
                        Question = "What type of projects will you manage?",
                        Type = "multiselect",
                        Required = true,
                        Order = 2,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "software", Label = "Software Development" },
                            new QuestionOption { Value = "marketing", Label = "Marketing Campaigns" },
                            new QuestionOption { Value = "operations", Label = "Operations & Process" },
                            new QuestionOption { Value = "research", Label = "Research & Development" }
                        }
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "Atlassian",
                    BusinessUnit = "Engineering",
                    Tags = new List<string> { "project-management", "collaboration", "development" },
                    EstimatedCost = 1200,
                    Currency = "USD"
                }
            },

            // HARDWARE PRODUCTS
            new Product
            {
                Id = "prod_laptop_dell",
                Name = "Dell XPS 15 Developer Laptop",
                Description = "High-performance laptop for development work with 32GB RAM, 1TB SSD",
                Category = "hardware",
                Price = 2500,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
                {
                    Level1 = new ApprovalLevel
                    {
                        Required = true,
                        ApproverType = "manager",
                        TimeoutHours = 48,
                        TriggerConditions = new TriggerConditions
                        {
                            MinAmount = 1000,
                            MaxAmount = 5000
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
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_specifications",
                        Key = "specifications",
                        Question = "Required specifications",
                        Type = "textarea",
                        Required = true,
                        Order = 1,
                        HelpText = "List specific hardware requirements (CPU, RAM, Storage, Graphics, etc.)"
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_replacement_reason",
                        Key = "replacementReason",
                        Question = "Reason for hardware request",
                        Type = "select",
                        Required = true,
                        Order = 2,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "new_employee", Label = "New Employee Setup" },
                            new QuestionOption { Value = "replacement", Label = "Replacing Failed Hardware" },
                            new QuestionOption { Value = "upgrade", Label = "Performance Upgrade" },
                            new QuestionOption { Value = "additional", Label = "Additional Equipment" }
                        }
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_warranty",
                        Key = "warrantyRequired",
                        Question = "Extended warranty needed?",
                        Type = "select",
                        Required = true,
                        Order = 3,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "standard", Label = "Standard 1-year warranty" },
                            new QuestionOption { Value = "extended_2", Label = "2-year extended warranty" },
                            new QuestionOption { Value = "extended_3", Label = "3-year extended warranty" },
                            new QuestionOption { Value = "premium", Label = "Premium support with on-site service" }
                        }
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "Dell Technologies",
                    BusinessUnit = "IT",
                    Tags = new List<string> { "laptop", "development", "high-performance" },
                    EstimatedCost = 2500,
                    Currency = "USD"
                }
            },

            new Product
            {
                Id = "prod_monitor_ultrawide",
                Name = "34\" Ultrawide Monitor",
                Description = "Professional ultrawide monitor for enhanced productivity",
                Category = "hardware",
                Price = 800,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
                {
                    Level1 = new ApprovalLevel
                    {
                        Required = true,
                        ApproverType = "manager",
                        TimeoutHours = 24,
                        TriggerConditions = new TriggerConditions
                        {
                            MinAmount = 300,
                            MaxAmount = 1500
                        }
                    }
                },
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_monitor_purpose",
                        Key = "purpose",
                        Question = "Primary use case for this monitor",
                        Type = "select",
                        Required = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "development", Label = "Software Development" },
                            new QuestionOption { Value = "design", Label = "Graphic Design" },
                            new QuestionOption { Value = "data_analysis", Label = "Data Analysis" },
                            new QuestionOption { Value = "general", Label = "General Productivity" }
                        }
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "LG Electronics",
                    BusinessUnit = "IT",
                    Tags = new List<string> { "monitor", "productivity", "ultrawide" },
                    EstimatedCost = 800,
                    Currency = "USD"
                }
            },

            // TRAINING PRODUCTS
            new Product
            {
                Id = "prod_aws_certification",
                Name = "AWS Cloud Certification Training",
                Description = "Comprehensive training program for AWS cloud certifications",
                Category = "training",
                Price = 1500,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
                {
                    Level1 = new ApprovalLevel
                    {
                        Required = true,
                        ApproverType = "manager",
                        TimeoutHours = 24,
                        TriggerConditions = new TriggerConditions
                        {
                            MinAmount = 500,
                            MaxAmount = 3000
                        }
                    }
                },
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_certification_type",
                        Key = "certificationType",
                        Question = "Which AWS certification are you pursuing?",
                        Type = "select",
                        Required = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "cloud_practitioner", Label = "AWS Cloud Practitioner" },
                            new QuestionOption { Value = "solutions_architect", Label = "Solutions Architect Associate" },
                            new QuestionOption { Value = "developer", Label = "Developer Associate" },
                            new QuestionOption { Value = "sysops", Label = "SysOps Administrator" },
                            new QuestionOption { Value = "professional", Label = "Professional Level Certification" }
                        }
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_career_relevance",
                        Key = "careerRelevance",
                        Question = "How does this certification align with your role?",
                        Type = "textarea",
                        Required = true,
                        Order = 2,
                        Validation = new QuestionValidation
                        {
                            Length = new LengthValidation { Min = 100, Max = 300 }
                        }
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "AWS Training Partners",
                    BusinessUnit = "Engineering",
                    Tags = new List<string> { "certification", "cloud", "professional-development" },
                    EstimatedCost = 1500,
                    Currency = "USD"
                }
            },

            // SERVICES PRODUCTS
            new Product
            {
                Id = "prod_security_consulting",
                Name = "Cybersecurity Consulting Services",
                Description = "Professional cybersecurity assessment and consulting",
                Category = "services",
                Price = 20000,
                Currency = "USD",
                IsActive = true,
                ApprovalModel = new ApprovalModel
                {
                    Level1 = new ApprovalLevel
                    {
                        Required = true,
                        ApproverType = "manager",
                        TimeoutHours = 48,
                        TriggerConditions = new TriggerConditions
                        {
                            MinAmount = 5000,
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
                            MinAmount = 15000
                        }
                    }
                },
                Questions = new List<ProductQuestion>
                {
                    new ProductQuestion
                    {
                        QuestionId = "q_service_scope",
                        Key = "serviceScope",
                        Question = "Scope of consulting services needed",
                        Type = "multiselect",
                        Required = true,
                        Order = 1,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "penetration_testing", Label = "Penetration Testing" },
                            new QuestionOption { Value = "vulnerability_assessment", Label = "Vulnerability Assessment" },
                            new QuestionOption { Value = "policy_review", Label = "Security Policy Review" },
                            new QuestionOption { Value = "compliance_audit", Label = "Compliance Audit" },
                            new QuestionOption { Value = "incident_response", Label = "Incident Response Planning" }
                        }
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_timeline",
                        Key = "timeline",
                        Question = "Expected project timeline",
                        Type = "select",
                        Required = true,
                        Order = 2,
                        Options = new List<QuestionOption>
                        {
                            new QuestionOption { Value = "1_month", Label = "1 Month" },
                            new QuestionOption { Value = "3_months", Label = "3 Months" },
                            new QuestionOption { Value = "6_months", Label = "6 Months" },
                            new QuestionOption { Value = "ongoing", Label = "Ongoing Engagement" }
                        }
                    },
                    new ProductQuestion
                    {
                        QuestionId = "q_deliverables",
                        Key = "expectedDeliverables",
                        Question = "Expected deliverables",
                        Type = "textarea",
                        Required = true,
                        Order = 3,
                        HelpText = "Describe what reports, documentation, or outcomes you expect"
                    }
                },
                Metadata = new ProductMetadata
                {
                    Vendor = "SecureConsult Inc.",
                    BusinessUnit = "IT Security",
                    Tags = new List<string> { "security", "consulting", "compliance" },
                    EstimatedCost = 20000,
                    Currency = "USD"
                }
            }
        };
    }
}
