# **Standard Operating Procedure: Project Initiation**

**Objective:** This document outlines the standardized procedures for initiating new projects within the organization. It provides distinct pathways for Greenfield (new) and Brownfield (existing) projects to ensure proper documentation and structural alignment.

## **1.0 Greenfield Project Initiation**

Greenfield projects are defined as new initiatives that are not built upon existing work. The successful initiation of a Greenfield project is contingent upon the submission of comprehensive planning documentation.

**Prerequisites:**

* **Business and Technical Requirements Document (BRD/TRD):** A formal document detailing the project's business objectives, outcomes, technical specifications, architecture, and required resources.

For guidance on creating this document, a standardized template is available.

* **Access Template:** [**Click Here**](assets/Template.pdf)   
<!-- * ![Template_demo](assets/Images/Template_demo.png) -->
<img src = "assets/Images/Template_demo.png" alt="Template_demo" width ="700" height ="300">

## **2.0 Brownfield Project Integration**

Brownfield projects involve the development or integration of pre-existing systems or codebases. The primary step is to import the existing project structure into the designated blueprinting environment.

**Procedure:**

1. Navigate to the **Blueprinting** module.  
2. Select the **Project Structure** option.  
3. Upload the compressed file containing the existing project architecture.  
* Click on Blueprinting tab.
<!-- * ![Blueprinting_demo](assets/Images/Blueprinting_demo.png) -->
<img src = "assets/Images/Blueprinting_demo.png" alt="Blueprinting_demo" width ="700" height ="300">
<!-- * Inside **Blueprinting** tab, Click on **Project Structure**. -->
* ![ProjectStructure_demo](assets/Images/ProjectStructure_demo.png)
<img src = "assets/Images/ProjectStructure_demo.png" alt="ProjectStructure_demo" width ="700" height ="300">
* Inside **Project Structure**, Click on **Upload Structure**.
<!-- * ![UploadStructure_demo](assets/Images/UploadStructure_demo.png) -->
<img src = "assets/Images/UploadStructure_demo.png" alt="UploadStructure_demo" width ="700" height ="300">

## **3.0 Accepted Submission Formats**

All project structures must be submitted as a compressed .zip archive. Two primary formats are accepted, as detailed below.

### **3.1 Solution-Level Archive**

This format encapsulates the entire solution within a single root folder. It is the preferred format for multi-project solutions.
<!-- ![SolutionZip_demo](assets/Images/SolutionZip_demo.png) -->
<img src = "assets/Images/SolutionZip_demo.png" alt="" width ="700" height ="300">

**Directory Structure Example:**

solution-structure.zip  
└── solution-folder/  
    ├── project-A/  
    │   ├── file1.cs  
    │   └── project-A.csproj  
    ├── project-B/  
    │   ├── file2.js  
    │   └── package.json  
    └── solution.sln

### **3.2 Project-Level Archive**

This format is suitable for single-project submissions or when a parent solution file is not applicable. The project folders are located at the root of the archive.
<!-- ![ProjectZip_demo](assets/Images/ProjectZip_demo.png) -->
<img src = "assets/Images/ProjectZip_demo.png" alt="" width ="700" height ="300">


**Directory Structure Example:**

project-structure.zip  
├── project-A/  
│   ├── file1.cs  
│   └── project-A.csproj  
└── project-B/  
    ├── file2.js  
    └── package.json  
