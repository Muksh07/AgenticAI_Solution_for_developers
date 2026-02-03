// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using backend.Database;
// using backend.Models;
// using backend.Service;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;

// namespace backend.Controllers.Database
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class DatabaseController : ControllerBase
//     {

//         private readonly HttpClient _httpClient;
//          private readonly DatabaseContext _context;

//         private readonly DabaseFunctionality _DabaseFunctionality;

//         public DatabaseController(HttpClient httpClient, DabaseFunctionality DabaseFunctionality, DatabaseContext db)
//         {
//             _httpClient = httpClient;
//             _DabaseFunctionality = DabaseFunctionality;
//              _context = db;
//         }

        

//         [HttpPost("createproject")]
//         public async Task<ActionResult<ProjectLifecycle>> CreateProject(ProjectLifecycle projectLifecycle)
//         {
//             try
//             {
//                 // _logger.LogInformation($"Receiving new project: {projectLifecycle.ProjectName}");

//                 // Set server-side timestamps
//                 projectLifecycle.CreatedDate = DateTime.UtcNow;
//                 projectLifecycle.LastUpdated = DateTime.UtcNow;

//                 _context.ProjectLifecycles.Add(projectLifecycle);
//                 await _context.SaveChangesAsync();

//                 //_logger.LogInformation($"Project created with ID: {projectLifecycle.ProjectID}");

//                 return CreatedAtAction(nameof(GetProject),
//                     new { id = projectLifecycle.ProjectID }, projectLifecycle);
//             }
//             catch (Exception ex)
//             {
//                 //_logger.LogError(ex, "Error creating project");
//                 return BadRequest($"Error creating project: {ex.Message}");
//             }
//         }

//         // PUT: api/projectlifecycle/5
//         [HttpPut("{id}")]
//         public async Task<IActionResult> UpdateProject(int id, ProjectLifecycle projectLifecycle)
//         {
//             try
//             {
//                 if (id != projectLifecycle.ProjectID)
//                 {
//                     return BadRequest("ID mismatch");
//                 }

//                 // //_logger.LogInformation($"Updating project ID: {id}, Status Updates: " +
//                 //     $"Insight: {projectLifecycle.InsightElicitationStatus}, " +
//                 //     $"Solidification: {projectLifecycle.SolidificationStatus}, " +
//                 //     $"Blueprinting: {projectLifecycle.BlueprintingStatus}, " +
//                 //     $"CodeSynthesis: {projectLifecycle.CodeSynthesisStatus}");

//                 var existingProject = await _context.ProjectLifecycles.FindAsync(id);
//                 if (existingProject == null)
//                 {
//                     return NotFound($"Project with ID {id} not found");
//                 }

//                 // Update all fields
//                 existingProject.ProjectName = projectLifecycle.ProjectName;
//                 existingProject.ProjectType = projectLifecycle.ProjectType;
//                 existingProject.CreatedBy = projectLifecycle.CreatedBy;
//                 existingProject.InsightElicitationStatus = projectLifecycle.InsightElicitationStatus;
//                 existingProject.SolidificationStatus = projectLifecycle.SolidificationStatus;
//                 existingProject.BlueprintingStatus = projectLifecycle.BlueprintingStatus;
//                 existingProject.CodeSynthesisStatus = projectLifecycle.CodeSynthesisStatus;
//                 existingProject.Code = projectLifecycle.Code;
//                 existingProject.description = projectLifecycle.description;
//                 existingProject.Testing_Unit = projectLifecycle.Testing_Unit;
//                 existingProject.Testing_Functional = projectLifecycle.Testing_Functional;
//                 existingProject.Testing_Integration = projectLifecycle.Testing_Integration;
//                 existingProject.Doc_HLD = projectLifecycle.Doc_HLD;
//                 existingProject.Doc_LLD = projectLifecycle.Doc_LLD;
//                 existingProject.Doc_UserManual = projectLifecycle.Doc_UserManual;
//                 existingProject.Doc_TraceabilityMatrix = projectLifecycle.Doc_TraceabilityMatrix;
//                 existingProject.CodeReview = projectLifecycle.CodeReview;
//                 existingProject.LastUpdated = DateTime.UtcNow; // Server timestamp

//                 _context.Entry(existingProject).State = EntityState.Modified;
//                 await _context.SaveChangesAsync();

//                 //_logger.LogInformation($"Project {id} updated successfully");

//                 return Ok(existingProject);
//             }
//             catch (Exception ex)
//             {
//                 //_logger.LogError(ex, $"Error updating project {id}");
//                 return BadRequest($"Error updating project: {ex.Message}");
//             }
//         }

//         // GET: api/projectlifecycle/5
//         [HttpGet("{id}")]
//         public async Task<ActionResult<ProjectLifecycle>> GetProject(int id)
//         {
//             try
//             {
//                 var project = await _context.ProjectLifecycles.FindAsync(id);

//                 if (project == null)
//                 {
//                     return NotFound();
//                 }

//                 //_logger.LogInformation($"Retrieved project: {project.ProjectName}");
//                 return project;
//             }
//             catch (Exception ex)
//             {
//                 //_logger.LogError(ex, $"Error retrieving project {id}");
//                 return BadRequest($"Error retrieving project: {ex.Message}");
//             }
//         }


//         [HttpPost("submitFeedback")]
//         public async Task<IActionResult> SubmitFeedback([FromBody] ProjectFeedback feedback)
//         {
//             try
//             {                
//                 feedback.FeedbackDate = DateTime.UtcNow;
//                 _context.ProjectFeedbacks.Add(feedback);
//                 await _context.SaveChangesAsync();
//                 return Ok(new { message = "Feedback submitted successfully." });
//             }
//             catch (Exception ex)
//             {
//                 // _logger.LogError(ex, "Error submitting feedback");
//                 return StatusCode(500, "An error occurred while submitting feedback.");
//             }
//         }
 




//     }
// }