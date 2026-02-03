// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using backend.Database;
// using backend.Models;
// using Microsoft.EntityFrameworkCore;

// namespace backend.Service
// {
//     public class DabaseFunctionality
//     {
//         private readonly DatabaseContext _db;
//         public DabaseFunctionality(DatabaseContext db)
//         {
//             _db = db;
//         }

//         // public async Task<ProjectLifecycle> SaveOrUpdateProjectAsync(ProjectLifecycle project)
//         // {
//         //     var existingProject = await _db.ProjectLifecycles
//         //                                    .FirstOrDefaultAsync(p => p.ProjectID == project.ProjectID);

//         //     if (existingProject == null)
//         //     {
//         //         // New project → add directly
//         //         _db.ProjectLifecycles.Add(project);
//         //     }
//         //     else
//         //     {
//         //         // Update only the fields which have values (partial update support)
//         //         existingProject.ProjectName = string.IsNullOrEmpty(project.ProjectName) ? existingProject.ProjectName : project.ProjectName;
//         //         existingProject.ProjectType = string.IsNullOrEmpty(project.ProjectType) ? existingProject.ProjectType : project.ProjectType;
//         //         existingProject.CreatedDate = project.CreatedDate ?? existingProject.CreatedDate;
//         //         existingProject.CreatedBy = string.IsNullOrEmpty(project.CreatedBy) ? existingProject.CreatedBy : project.CreatedBy;
//         //         existingProject.InsightElicitationStatus = string.IsNullOrEmpty(project.InsightElicitationStatus) ? existingProject.InsightElicitationStatus : project.InsightElicitationStatus;
//         //         existingProject.SolidificationStatus = string.IsNullOrEmpty(project.SolidificationStatus) ? existingProject.SolidificationStatus : project.SolidificationStatus;
//         //         existingProject.BlueprintingStatus = string.IsNullOrEmpty(project.BlueprintingStatus) ? existingProject.BlueprintingStatus : project.BlueprintingStatus;
//         //         existingProject.CodeSynthesisStatus = string.IsNullOrEmpty(project.CodeSynthesisStatus) ? existingProject.CodeSynthesisStatus : project.CodeSynthesisStatus;
//         //         existingProject.Code = string.IsNullOrEmpty(project.Code) ? existingProject.Code : project.Code;
//         //         existingProject.description = string.IsNullOrEmpty(project.description) ? existingProject.description : project.description;

//         //         _db.ProjectLifecycles.Update(existingProject);
//         //     }

//         //     await _db.SaveChangesAsync();
//         //     return project;
//         // }
        







//         public async Task<int> SaveOrUpdateProjectAsync(int? id, string task)
//         {
//             var existingProject = await _db.ProjectLifecycles.FindAsync(id);
//             if (existingProject == null)
//                 {
//                     Console.WriteLine($"Project with ID {id} not found");
//                 }


//             Console.WriteLine("id", id);
//             Console.WriteLine("task", task);
//             // Update only the fields which have values (partial update support)
//             if (task == "AnalyzeBRD")
//             {
//                 existingProject.InsightElicitationStatus = "Completed";
//             }
//             else if (task == "SolidifyBRD")
//             {
//                 existingProject.SolidificationStatus = "Completed";
//             }
            
//             else if (task == "Blueprinting")
//             {

//                 existingProject.BlueprintingStatus = "Completed";

//             }
//             else if (task == "green")
//             {
//                 existingProject.ProjectType = "Greenfield";
//             }
//             else if (task == "brown")
//             {
//                 existingProject.ProjectType = "Brownfield";

//             }
//             else if (task == "CodeSynth")
//             {
//                 existingProject.CodeSynthesisStatus = "Completed";
//             }
//             else if (task == "code")
//             {
//                 existingProject.Code = "Completed";
//             }
//             else if (task == "Testing_Unit")
//             {
//                 existingProject.Testing_Unit = "Completed";
//             }
//             else if (task == "Testing_Functional")
//             {
//                 existingProject.Testing_Functional = "Completed";

//             }
//             else if (task == "Testing_Integration")
//             {
//                 existingProject.Testing_Integration = "Completed";
//             }
//             else if (task == "Doc_LLD")
//             {
//                 existingProject.Doc_LLD = "Completed";
//             }
//             else if (task == "Doc_HLD"){
//                 existingProject.Doc_HLD = "Completed";
//             }
//             else if (task == "Doc_UserManual")
//             {
//                 existingProject.Doc_UserManual = "Completed";
//             }
//             else if (task == "codeReview")
//             {
//                 existingProject.CodeReview = "Completed";
//             }
//             else if (task == "Doc_TraceabilityMatrix")
//             {
//                 existingProject.Doc_TraceabilityMatrix = "Completed";
//             }
//             else if (task == "description")
//             {
//                 existingProject.description = "Completed";
//             }
                
//             existingProject.LastUpdated = DateTime.UtcNow;
//             _db.Entry(existingProject).State = EntityState.Modified;
//             await _db.SaveChangesAsync();
//             return 0;
//         }
        
            







//     }
// }