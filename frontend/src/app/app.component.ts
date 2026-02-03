import { ChangeDetectorRef, Component, ElementRef, HostListener, Renderer2, ViewChild, ɵprovideZonelessChangeDetection } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../Services/api.service';
import { Subject, Subscription, take, takeUntil, firstValueFrom, empty } from 'rxjs';
import { saveAs } from 'file-saver';
import JSZip, { JSZipObject } from 'jszip';
import { Router, RouterModule } from '@angular/router';
import { environment } from '../../environments/environment.prod';
import { CodeEditor } from '@acrodata/code-editor';
import { HttpClient } from '@angular/common/http';
import { marked } from 'marked';
// import { FeedbackFormComponent } from './feedback-form/feedback-form.component'; 
// import mammoth from 'mammoth';
// import * as mammoth from 'mammoth';

export interface Node {
  name: string;
  type: 'folder' | 'file';
  expanded: true;
  content?: string;          // files only
  children?: Node[];         // folders only
}


// project-lifecycle.model.ts
export interface ProjectLifecycle {
  projectID: number;
  projectName: string;
  projectType?: string;
  createdDate?: Date;
  createdBy?: string;
  insightElicitationStatus?: string;
  solidificationStatus?: string;
  blueprintingStatus?: string;
  codeSynthesisStatus?: string;
  code?: string;
  testing_Unit?: string;
  testing_Functional?: string;
  testing_Integration?: string;
  doc_HLD?: string;
  doc_LLD?: string;
  doc_UserManual?: string;
  doc_TraceabilityMatrix?: string;
  codeReview?: string;
  description?: string;
  lastUpdated?: Date;
}

// project-feedback.model.ts
export interface ProjectFeedback {
  feedbackID?: number;
  projectID: number;
  codeCoverageScore?: number;
  codeQualityScore?: number;
  artifactQuality?: string;
  reviewerComments?: string;
  feedbackDate?: Date;
}

export interface FileSystemNode {
  name: string;
  type: 'file' | 'folder';
  content: string;          // always empty  ➜  ""
  code: string;             // the file’s text (files) or "" (folders)
  expanded: boolean;
  children?: FileSystemNode[];
}








//find and search
type Area = 'input' | 'output' | 'tech';

//validate template
const SECTIONS_TEMPLATE = environment.SECTIONS_TEMPLATE;
const MANDATORY_MASK = environment.MANDATORY_MASK;
const TECHNICALCOMMONTERMS = environment.technicalCommonTerms;
const INSIGHTCOMMONTERMS = environment.Insight_common_terms;
const MandatoryFolders = environment.folderName;

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
  imports: [CommonModule, FormsModule, RouterModule, CodeEditor,FormsModule]  // ✅ Add this to imports array],
})
export class AppComponent {
  activeTab: string = 'Insight Elicitation';
  activeBlueprintingSubTab: string = 'Requirement Summary';

  //reverseengineering


  fileTree: FileSystemNode[] = [];            // final JSON you asked for

  private readonly TEXT_EXTS = new Set<string>([
  /* plain text & docs */ 'txt', 'log', 'csv', 'tsv', 'md', 'markdown', 'rst', 'rtf',
  /* data / markup    */ 'json', 'jsonc', 'xml', 'yml', 'yaml', 'toml', 'ini', 'cfg', 'conf', 'properties',
  /* web / markup     */ 'html', 'htm', 'xhtml', 'svg', 'css', 'scss', 'sass', 'less',
  /* scripts          */ 'sh', 'bash', 'zsh', 'ksh', 'bat', 'cmd', 'ps1', 'psm1', 'psd1',
  /* JavaScript land  */ 'js', 'mjs', 'cjs', 'jsx', 'ts', 'tsx',
    /* compiled langs   */
    'c', 'h', 'cpp', 'cxx', 'cc', 'c++', 'hpp', 'hxx', 'hh', 'h++',
    'cs', 'csx', 'java', 'kt', 'kts', 'groovy', 'gvy', 'gy', 'gsh',
    'go', 'rs', 'swift', 'scala', 'sc', 'dart', 'd',
    'rb', 'erb', 'pl', 'pm', 'pod', 't', 'php', 'phtml', 'php3', 'php4', 'php5', 'phps',
    'py', 'pyw', 'pyx', 'r', 's', 'S', 'asm', 'ex', 'exs', 'erl', 'hrl', 'hs', 'lua',
    'ml', 'mli', 'm', 'mm', 'ada', 'adb', 'ads', 'f', 'f77', 'f90', 'f95', 'f03', 'sql',
  /* build / infra    */ 'gradle', 'cmake', 'tf', 'tfvars',
  /* TeX & co         */ 'tex', 'sty', 'cls', 'config', 'csproj',
    'cache'
  ]);

  /** Filenames that have *no* extension yet are plain-text */
  private readonly TEXT_FILENAMES = new Set<string>([
    'makefile', 'mak', 'mkfile',
    'dockerfile',
    'cmakelists.txt',
    '.gitignore', '.gitattributes', '.gitmodules',
    '.editorconfig', '.env', '.dotenv',
    'docker-compose.yaml', 'docker-compose.yml'
  ]);

  //feedback variables
  BRDfeedback: string = '';
  SOLfeedback: string = '';
  BLUfeedback: string = '';
  CODfeedback: string = '';

  
 projectLifecycleData: ProjectLifecycle = {
    projectID: 0,
    projectName: 'UnDefined',
    projectType: '',
    createdDate: new Date(),
    createdBy: 'Current User',
    insightElicitationStatus: 'In Progress',
    solidificationStatus: 'In Progress',
    blueprintingStatus: 'In Progress',
    codeSynthesisStatus: 'In Progress',
    code: '',
    testing_Unit: '',
    testing_Functional: '',
    testing_Integration: '',
    doc_HLD: '',
    doc_LLD: '',
    doc_UserManual: '',
    doc_TraceabilityMatrix: '',
    codeReview: '',
    description: '',
    lastUpdated: new Date()
  };





  // common variable disable
  apicallvariable: boolean = false;



  //dynamic textarea
  private resizingSection: 'input' | 'output' | 'folder' | 'content' | null = null;
  private minSectionWidth: number = 20; // Minimum width percentage

  private minWidthPercentage = 20;

  //helpers
  inprojectstructure: boolean = false;
  enableReverseEngineering: boolean = false;
  brownfield: boolean = false;

  //validate template
  outputPlaceholder = '';
  brdplaceholder = '';

  // Properties for the Blueprinting sub-tabs
  requirementSummary: string = '';
  solutionOverview: string = '';
  projectStructure: string = '';
  projectStructureTemplate: string = '';
  dataFlow: string = '';
  unitTesting: string = '';
  FunctionalTesting: string = '';
  IntegrationTesting: string = '';
  commonFunctionalities: string = '';
  databaseScripts: string = '';
  parsedStructure: any;
  selectedContent: string = '';
  folderStructureboolean: boolean = false;
  structureuploaded: boolean = false;

  // tree variables
  projectStructureDescription: string = '';
  folderStructure: any[] = [];
  selectedFileContent: string = '';




  taskInput: string = '';
  promptLimit: number = 2048;
  tokenLimit: number = 4096;

  // Other properties
  insidestep1: boolean = true;
  insidestep2: boolean = false;
  insidestep3: boolean = false;
  insidestep4: boolean = false;
  inputText: string = '';
  outputText: string = '';
  outputText2: string = '';
  outputText3: string = '';
  outputText4: string = '';
  tokenCount: number = 0;
  completionTokens: number = 0;
  promptTokens: number = 0;
  totalTokens: number = 0;
  technicalRequirement: string = '';
  solutionDesign: string = '';
  blueprintingContent: string = '';

  // Property for Code Synthesis
  codeSynthesisContent: string = '';
  isAnalyzing: boolean = false;
  response: boolean = false;
  codeSynthesisFolderStructure: any[] = [];
  selectedCodeFile: string = '';
  selectedCodeContent: string = '';
  unittesttree: any;
  databasetree: any;
  datascripttree: any;
  unittestingtree: any;
  functiontestingtree: any;
  Integrationtestingtree: any;
  loading: boolean = false;
  isdescribe: boolean = false;
  iscodeReview: boolean = false;
  codeSynthesisFolderStructureboolean: boolean = false;

  //variables for processing
  isAnalyzingCOD: boolean = false;
  responseCOD: boolean = false;
  isAnalyzingBRD: boolean = false;
  responseBRD: boolean = false;
  isAnalyzingSOL: boolean = false;
  responseSOL: boolean = false;
  isAnalyzingBLU: boolean = false;
  responseBLU: boolean = false;


  // Properties to track selected nodes
  selectedTreeNode: any = null;
  selectedCodeTreeNode: any = null;
  selectedCodeTreeNodeType: string = '';



  //codesynthesis
  showDescription = false;
  istraversing: boolean = true;


  //subscribe
  private apiBRD: Subscription | undefined;
  private apiREV: Subscription | undefined;
  private apiSOL: Subscription | undefined;
  private apiBLU: Subscription | undefined;
  private apiCOD: Subscription | undefined;
  private apiSUM: Subscription | undefined;
  private apiTM: Subscription | undefined;

  private abortController: AbortController | undefined;
  isVisible: boolean = false;
  private cPressed: boolean = false;
  private oPressed: boolean = false;
  private dPressed: boolean = false;


  //abort
  abortedBRD: boolean = false;
  abortedREV: boolean = false;
  abortedSOL: boolean = false;
  abortedBLU: boolean = false;
  abortedCOD: boolean = false;

  //traversing 
  private cancel$ = new Subject<void>();
  private abortCtrl = new AbortController();

  templates: { [key: string]: string } = {};


  //documentation  
  activeTabDocumentation: string = 'hld';
  templateContent: string = '';
  hldtemplate: string = "";
  lldtemplate: string = "";
  usermanualtemplate: string = "";
  TraceabilityMatrixtemplate: string = "";





  //code review 
  // CodeReviewContent: string = '';



  activeTabCodeReview: string = 'UploadChecklist';
  UploadChecklist: string = "";
  UplaodBestPractice: string = "";
  EnterLanguageType: string = "";
  currentProcessingFile: any;
  solutionOverview2: any;

  //   CodeReviews = {
  //   UploadChecklist: '',
  //   UploadBestPractice: '',
  //   EnterLanguageType: '',
  // };


  // ✅ Two-way binding through getter/setter
  get CodeReviewContent(): string {
    switch (this.activeTabCodeReview) {
      case 'UploadChecklist':
        return this.UploadChecklist;
      case 'UploadBestPractice':
        return this.UplaodBestPractice;
      case 'EnterLanguageType':
        return this.EnterLanguageType;
      default:
        return '';
    }
  }

  set CodeReviewContent(val: string) {
    switch (this.activeTabCodeReview) {
      case 'UploadChecklist':
        this.UploadChecklist = val;
        // this.CodeReviews.UploadChecklist = val;
        break;
      case 'UploadBestPractice':
        this.UplaodBestPractice = val;
        // this.CodeReviews.UploadBestPractice = val;
        break;
      case 'EnterLanguageType':
        this.EnterLanguageType = val;
        // this.CodeReviews.EnterLanguageType = val;
        break;
    }
  }


  dropdownOpen: boolean = false;


showFeedbackForm: boolean = false;

  openFeedbackForm() {
    if (this.projectLifecycleData.projectID && this.projectLifecycleData.projectID > 0) {
      this.showFeedbackForm = true;
    } else {
      alert('Please create or load a project first.');
    }
  }

  closeFeedbackForm() {
    this.showFeedbackForm = false;
  }

 onFeedbackSubmitted(feedback: any) {
    console.log('✅ Feedback submitted:', feedback);
    // Remove this line: alert('🎉 Feedback submitted successfully!');
    this.showFeedbackForm = false;
    
    // Optionally refresh project data or update UI
    // this.syncToBackend('Feedback Submitted');
  }


  onFileUpload(subtab: string, event: Event): void {
    const input = event.target as HTMLInputElement;

    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();

      reader.onload = () => {
        const fileContent = reader.result as string;

        switch (subtab) {
          case 'UploadChecklist':
            this.UploadChecklist = fileContent;
            //this.CodeReviews.UploadChecklist = fileContent;
            break;
          case 'UploadBestPractice':
            this.UplaodBestPractice = fileContent;
            //this.CodeReviews.UploadBestPractice = fileContent;
            break;
          case 'EnterLanguageType':
            this.EnterLanguageType = fileContent;
            //this.CodeReviews.EnterLanguageType = fileContent;
            break;
        }

        // Show the uploaded content immediately
        this.activeTabCodeReview = subtab;
      };

      reader.readAsText(file);
    }
  }


  // Declare the boolean variable
  generateBRDWithTRD: boolean = false;

  // Method to handle the button click

resetProjectCycleDataToDefault(){
  this.projectLifecycleData = {
    projectID: 0,
    projectName: 'UnDefined',
    projectType: '',
    createdDate: new Date(),
    createdBy: 'Current User',
    insightElicitationStatus: 'In Progress',
    solidificationStatus: 'In Progress',
    blueprintingStatus: 'In Progress',
    codeSynthesisStatus: 'In Progress',
    code: '',
    testing_Unit: '',
    testing_Functional: '',
    testing_Integration: '',
    doc_HLD: '',
    doc_LLD: '',
    doc_UserManual: '',
    doc_TraceabilityMatrix: '',
    codeReview: '',
    description: '',
    lastUpdated: new Date()
  };
  console.log('📊 Current project progress:', this.projectLifecycleData);
}

  settodefault(i: number) {
    // Reset common properties
    const resetCommonProperties = () => {
      this.technicalRequirement = "";
      this.solutionOverview = "";
      this.dataFlow = "";
      this.commonFunctionalities = "";
      this.projectStructure = "";
      this.requirementSummary = "";
      this.unitTesting = "";
      this.databaseScripts = "";
      this.codeSynthesisFolderStructure = [];
      this.folderStructure = [];
      this.isAnalyzingBLU = false;
      this.responseBLU = false;
      this.projectStructureDescription = "";
      this.selectedContent = "";
      this.selectedCodeContent = "";
      this.templates = {
        hld: "",
        lld: "",
        userManual: "",
        traceabilityMatrix: ""
      };
      this.templateContent = "";
      this.abortedBLU = false;
      this.abortedCOD = false;
      this.abortedSOL = false;
      this.abortedREV = false;
      this.abortedBRD = false;
      this.folderStructureboolean = false;
      this.codeSynthesisFolderStructureboolean = false;
      this.BRDfeedback = '';
      this.SOLfeedback = '';
      this.BLUfeedback = '';
      this.CODfeedback = '';
      this.UploadChecklist = "";
      this.EnterLanguageType = "";
      this.UplaodBestPractice = "";
      this.reverseEngineeringAlreadyCalled = false;
    };

    // Case specific actions
    if (i === 1) {
      if (this.insidestep1) {
        this.outputText = "";
      }
      this.outputText2 = "";
      resetCommonProperties();
    } else if (i === 2) {
      resetCommonProperties();
    } else if (i === 3) {
      if (this.selectedTabs['Data Flow']) {
        this.dataFlow = "";
      }
      if (this.selectedTabs['Common Functionalities']) {
        this.commonFunctionalities = "";
      }
      if (this.isGreenfield) {
        if (this.selectedTabs['Project Structure']) {
          this.projectStructure = "";
          this.projectStructureDescription = "";
          this.folderStructureboolean = false;
          this.folderStructure = [];
        }
        if (this.selectedTabs['Solution Overview']) {
          this.solutionOverview = "";
        }
      }
      if (this.selectedTabs['Requirement Summary']) {
        this.requirementSummary = "";
      }
      if (this.selectedTabs['Documentation']) {
        this.templates = {
          hld: "",
          lld: "",
          userManual: "",
          traceabilityMatrix: ""
        };
        this.templateContent = "";
      }
      if (this.selectedTabs['Unit Testing']) {
        this.unitTesting = "";
      }
      if (this.selectedTabs['Functional Testing']) {
        this.FunctionalTesting = "";
      }
      if (this.selectedTabs['Database Scripts']) {
        this.databaseScripts = "";
      }
      if (this.selectedTabs['Integration Testing']) {
        this.IntegrationTesting = "";
      }
      if (this.selectedTabs['Code Review']) {
        this.UploadChecklist = "";
        this.EnterLanguageType = "";
        this.UplaodBestPractice = "";
      }

      this.codeSynthesisFolderStructure = [];
      this.isAnalyzingBLU = false;
      this.responseBLU = false;
      this.selectedContent = "";
      this.selectedCodeContent = "";
      this.abortedBLU = false;
      this.abortedCOD = false;
      this.abortedSOL = false;
      this.abortedBRD = false;
      this.abortedREV = false;
      this.BLUfeedback = '';
      this.CODfeedback = '';
    }
  }


  constructor(private http: HttpClient, private apiService: ApiService, private renderer: Renderer2, public routes: Router, private cd: ChangeDetectorRef) {
    this.listenForKeys();
    
  }

  async initializeAndSaveProject() {
    this.projectLifecycleData.createdDate = new Date();
    this.projectLifecycleData.lastUpdated = new Date();
    await this.syncToBackend('Project Initialized');
  }

  // Add debugging to your sync method
  async syncToBackend(action: string): Promise<void> {
    console.log(`🔄 Starting sync for: ${action}`);
    console.log('📊 Current project data:', this.projectLifecycleData);
    
    try {
      // this.projectLifecycleData.lastUpdated = new Date();
      
      // if (this.projectLifecycleData.projectID === 0 || this.projectLifecycleData.projectID == null) {
      //   console.log('📤 Creating new project...');
      //   const response = await this.apiService.saveProjectLifecycle(this.projectLifecycleData).toPromise();
      //   console.log('📥 Create response:', response);
        
      //   if (response && response.projectID) {
      //     this.projectLifecycleData.projectID = response.projectID;
      //     console.log(`✅ ${action} - Project Created with ID: ${this.projectLifecycleData.projectID}`);
      //   }
      // } else {
      //   console.log(`📤 Updating project ID: ${this.projectLifecycleData.projectID}`);
      //   const response = await this.apiService.updateProjectLifecycle(
      //     this.projectLifecycleData.projectID, 
      //     this.projectLifecycleData
      //   ).toPromise();
      //   console.log('📥 Update response:', response);
      //   console.log(`✅ ${action} - Project Updated`);
      // }
    } catch (error: any) {
      console.error(`❌ Sync failed for ${action}:`, error);
      console.error('Error details:', {
        status: error.status,
        statusText: error.statusText,
        url: error.url,
        message: error.message
      });
    }
}

  // showDisclaimer = true;
  // disclaimerContent: any;
  // isLoadingDisclaimer = true;


  // loadDisclaimer() {
  //   this.http.get('assets/Disclaimer.md', { responseType: 'text' })
  //     .subscribe({
  //       next: (markdown: string) => {
  //         // Configure marked to handle images properly
  //         marked.setOptions({
  //           breaks: true,
  //           gfm: true
  //         });

  //         this.disclaimerContent = marked.parse(markdown);
  //         this.isLoadingDisclaimer = false;
  //       },
  //       error: (error) => {
  //         console.error('Error loading disclaimer:', error);
  //         this.disclaimerContent = '<p>Error loading disclaimer content.</p>';
  //         this.isLoadingDisclaimer = false;
  //       }
  //     });
  // }

  // closeDisclaimer() {
  //   this.showDisclaimer = false;
  // }
  ngOnInit() {

    // this.loadDisclaimer();
    // Default all to true (checked)
    this.initialiseSelection(this.codeSynthesisFolderStructure);
    this.blueprintingSubTabs.forEach(tab => {
      this.selectedTabs[tab] = this.alwaysSelectedTabs.includes(tab);
    });
    //  this.initialiseSelection(this.codeSynthesisFolderStructure);
    // this.blueprintingSubTabs.forEach(tab => {
    //   this.selectedTabs[tab] = true;
    // });
    this.apiService.getHldTemplate().subscribe(data => {
      this.hldtemplate = data;
    });

    this.apiService.getLldTemplate().subscribe(data => {
      this.lldtemplate = data;
    });

    this.apiService.getUserManualTemplate().subscribe(data => {
      this.usermanualtemplate = data;
    });

    this.apiService.getTraceabilityMatrixTemplate().subscribe(data => {
      this.TraceabilityMatrixtemplate = data;
    });
  }



  ngAfterViewInit(): void {
    this.refreshContent('input');
    this.refreshContent('output');
    this.refreshContent('tech');
    this.loadRecentSearches();
    // this.setupGlobalListeners();
  }


  ngOnDestroy() {
    Object.values(this.dropdownTimeouts).forEach(timeout => {
      if (timeout) clearTimeout(timeout);
    });

  }



  /* ────── Tab Management State ────── */
  /* ────── Replace outputTabData with direct property mapping ────── */
  activeOutputTab: 'output1' | 'output2' | 'output3'| 'output4' = 'output1';

  // Remove the old outputTabData object completely

  /* ========== DYNAMIC PLACEHOLDER METHOD ========== */
  getActiveTabPlaceholder(): string {

    if (this.activeOutputTab === 'output1'){
      return this.outputPlaceholder;
    }
    else if(this.activeOutputTab === 'output2'){
    return '';}
  else if(this.activeOutputTab === 'output3'){
    return '';
  }
    else{
      return '';
    }

  }


  /* ========== UPDATED TAB METHODS (REPLACE EXISTING) ========== */
  switchOutputTab(tab: 'output1' | 'output2' | 'output3' | 'output4'): void {
    if (tab === 'output2') {
      this.insidestep2 = true
      this.insidestep1 = false
      this.insidestep3 = false
      this.insidestep4 = false
    }
    else if (tab === 'output1') {
      this.insidestep2 = false
      this.insidestep1 = true
      this.insidestep3 = false
      this.insidestep4 = false


    }
    else if (tab === 'output3') {
      this.insidestep2 = false
      this.insidestep1 = false
      this.insidestep3 = true
      this.insidestep4 = false
    }
    else if (tab === 'output4') {
      this.insidestep2 = false
      this.insidestep1 = false
      this.insidestep3 = false
      this.insidestep4 = true


    }
    console.log("this.insidestep2", this.insidestep2)
    console.log("this.insidestep1", this.insidestep1)
    console.log("this.insidestep3", this.insidestep3)
    console.log("this.insidestep4", this.insidestep4)


    // Save current content before switching
    this.saveCurrentTabContent();

    // Switch to new tab
    this.activeOutputTab = tab;

    // Update the display
    setTimeout(() => {
      this.refreshContent('output');
    });
  }

  getActiveTabContent(): string {
    if (this.activeOutputTab === 'output1'){
      return this.outputText;
    }
    else if(this.activeOutputTab === 'output2')
      {
    return this.outputText2;
  }
    else if(this.activeOutputTab === 'output3')
      {
    return this.outputText3;
  }
    else{
      return this.outputText4;
    }
  }

  get activeTabContent(): string {
    if (this.activeOutputTab === 'output1'){
      return this.outputText;
    }
    else if(this.activeOutputTab === 'output2'){
    return this.outputText2;}
    else if(this.activeOutputTab === 'output3'){
    return this.outputText3;}
    else{
      return this.outputText4;
    }
  }


  set activeTabContent(value: string) {
    if (this.activeOutputTab === 'output1') {
      this.outputText = value;
    } else if (this.activeOutputTab === 'output2') {
      this.outputText2 = value;
    }
     else if (this.activeOutputTab === 'output3') {
      this.outputText3 = value;
    }
    else {
      this.outputText4 = value;
    }
  }

  private saveCurrentTabContent(): void {
    if (this.editMode.output) {
      // If in edit mode, save from textarea
      const textarea = this.outputTextarea?.nativeElement;
      if (textarea) {
        if (this.activeOutputTab === 'output1') {
          this.outputText = textarea.value;
        } else if (this.activeOutputTab === 'output2') {
          this.outputText2 = textarea.value;
        }
        else if (this.activeOutputTab === 'output3') {
          this.outputText3 = textarea.value;
        }
        else {
      this.outputText4 = textarea.value;
    }
      }
    }
  }




  // for solidification

// getActiveTechTabPlaceholder(): string {
//   return this.activeTechTab === 'requirement' 
//     ? 'Technical requirements and specifications will appear here...'
//     : 'Solution design and architecture details will appear here...';
// }

getActiveTechTabPlaceholder(): string {
    if (this.activeTechTab === 'requirement') {
    return ""
    }
    else if (this.activeTechTab === 'solution') {
    return ""
    }
    else {
      return ""
    }
    
}

/* ────── Tech Tab Management State ────── */
/* ────── Separate Tech Tab Variables ────── */
activeTechTab: 'requirement' | 'solution'|'solution3' = 'requirement';
techRequirement1 = '';  // Requirements tab content
techRequirement2 = '';  // Solution tab content
techRequirement3 = '';  // Solution tab content


/* ========== UPDATED TECH TAB METHODS WITH SEPARATE VARIABLES ========== */
switchTechTab(tab: 'requirement' | 'solution'| 'solution3'): void {
  // Save current content before switching
  this.saveCurrentTechTabContent();
  
  // Switch to new tab
  this.activeTechTab = tab;
  
  // Update the display
  setTimeout(() => {
    this.refreshContent('tech');
  });
}

getActiveTechTabContent(): string {

  if (this.activeTechTab === 'requirement') {
    return this.techRequirement1
    }
    else if (this.activeTechTab === 'solution') {
    return this.techRequirement2
    }
    else {
      return this.techRequirement3
    }
    
    
    
   
}

get activeTechTabContent(): string {
  if (this.activeTechTab === 'requirement') {
    return this.techRequirement1
    }
    else if (this.activeTechTab === 'solution') {
    return this.techRequirement2
    }
    else {
      return this.techRequirement3
    }
}

set activeTechTabContent(value: string) {
  if (this.activeTechTab === 'requirement') {
    this.techRequirement1 = value;
  } else if(this.activeTechTab === 'solution') {
    this.techRequirement2 = value;
  }
  else {
    this.techRequirement3 = value;
  }
}

private saveCurrentTechTabContent(): void {
  if (this.editMode.tech) {
    // If in edit mode, save from textarea
    const textarea = this.techTextarea?.nativeElement;
    if (textarea) {
      if (this.activeTechTab === 'requirement') {
        this.techRequirement1 = textarea.value;
      }
      else if(this.activeTechTab === 'solution'){
        this.techRequirement2 = textarea.value;
      }
      else {
        this.techRequirement3 = textarea.value;
      }
    }
  }
}













  // Project type selection
  showProjectSelection = true;  // Show selection first
  isGreenfield = false;
  isBrownfield = false;
  enteringProject = false;
  selectedType: 'greenfield' | 'brownfield' | null = null;

  // ... your existing properties ...


  // Project type selection methods
  // selectGreenfield() {
  //   this.isGreenfield = true;
  //   this.isBrownfield = false;
  //   this.showProjectSelection = false;
  //   // Set default tab for greenfield
  //   this.activeTab = 'Insight Elicitation';
  // }

  async selectGreenfield() {
    this.selectedType = 'greenfield';
    this.enteringProject = true;
    setTimeout(() => {
      this.isGreenfield = true;
      this.isBrownfield = false;
      this.showProjectSelection = false;
      this.enteringProject = false;
      this.activeTab = 'Insight Elicitation';
    }, 1200); // 1.2s animation
    // await this.initializeAndSaveProject();
    // this.projectLifecycleData.projectType = "Greenfield";
    // await this.syncToBackend('Project Type Completed');
  }

  // selectBrownfield() {
  //   this.isBrownfield = true;
  //   this.isGreenfield = false;
  //   this.showProjectSelection = false;
  //   // Set default tab for brownfield
  //   this.activeTab = 'Blueprinting';
  //   this.activeBlueprintingSubTab = "Project Structure"
  //   this.inprojectstructure = true;
  // }

  async selectBrownfield() {
    this.selectedType = 'brownfield';
    this.enteringProject = true;
    setTimeout(() => {
      this.isBrownfield = true;
      this.isGreenfield = false;
      this.showProjectSelection = false;
      this.enteringProject = false;
      this.activeTab = 'Blueprinting';
      this.activeBlueprintingSubTab = "Project Structure"
      this.inprojectstructure = true;
    }, 1200); // 1.2s animation
    //     await this.initializeAndSaveProject();
    // this.projectLifecycleData.projectType = "Brownfield";
    // await this.syncToBackend('Project Type Completed');
  }


  showConfirmationDialog = false;

  // ... existing methods ...

  // Modified method to show confirmation first
  confirmProjectTypeChange() {
    this.showConfirmationDialog = true;
  }

  // Confirm and go back
  confirmGoBack() {
    this.showConfirmationDialog = false;
    this.goBackToProjectSelection();
  }

  // Cancel confirmation
  cancelGoBack() {
    this.showConfirmationDialog = false;
  }
  goBackToProjectSelection() {
    this.settodefault(1)
    this.resetProjectCycleDataToDefault()
    this.inputText = "";
    this.showProjectSelection = true;
    this.isGreenfield = false;
    this.isBrownfield = false;
  }





  /* --------------------------------------------------
   Validate BRD
-------------------------------------------------- */

  //#region BRD validation
  //validate template
  requiredSections: string[] = SECTIONS_TEMPLATE
    .split(/[\n,]+|\s{2,}/)
    .map(s => s.trim())
    .filter(s => s.length > 0);

  mandatoryFlags: boolean[] = MANDATORY_MASK
    .trim()
    .split(/\s+/)
    .map(v => v === '1');

  private sleep = (ms: number) => new Promise(r => setTimeout(r, ms));

  private normalizeSection(s: string) {
    return s.replace(/[^a-zA-Z]/g, '').toLowerCase();
  }
  private normalizeText(t: string) {
    return t
      .replace(/^[\s]*([IVXLCDM]+|\d+|[A-Za-z])[\.\)\s-]*/gm, '')
      .replace(/[^a-zA-Z]/g, '')
      .toLowerCase();
  }

  private validateBRD(raw: string) {
    const hay = this.normalizeText(raw);

    const foundMandatory: string[] = [];
    const missingMandatory: string[] = [];
    const foundOptional: string[] = [];
    const missingOptional: string[] = [];

    this.requiredSections.forEach((sec, i) => {
      const ok = hay.includes(this.normalizeSection(sec));
      const list = this.mandatoryFlags[i]
        ? (ok ? foundMandatory : missingMandatory)
        : (ok ? foundOptional : missingOptional);
      list.push(sec);
    });

    const score =
      +((foundMandatory.length + foundOptional.length) /
        this.requiredSections.length * 100).toFixed(1);

    const pass = missingMandatory.length === 0;

    return {
      pass, score, foundMandatory, missingMandatory,
      foundOptional, missingOptional
    };
  }

  //#endregion







  /* --------------------------------------------------
     TaskINput helpers
  -------------------------------------------------- */

  //#region Task Input Shortcut
  listenForKeys(): void {
    document.addEventListener('keydown', this.onKeyDown.bind(this));
    document.addEventListener('keyup', this.onKeyUp.bind(this));
  }

  onKeyDown(event: KeyboardEvent): void {
    const key = event.key.toLowerCase();

    if (key === 'c') this.cPressed = true;
    if (key === 'o') this.oPressed = true;
    if (key === 'd') this.dPressed = true;


    if (this.cPressed && this.oPressed && this.dPressed) {
      this.isVisible = !this.isVisible;

      // Reset so it doesn’t keep toggling while holding
      this.cPressed = false;
      this.oPressed = false;
      this.dPressed = false;

    }
  }

  onKeyUp(event: KeyboardEvent): void {
    const key = event.key.toLowerCase();

    if (key === 'c') this.cPressed = false;
    if (key === 'o') this.oPressed = false;
    if (key === 'd') this.dPressed = false;






  }
  //#endregion








  /* --------------------------------------------------
     tabs Logic
  -------------------------------------------------- */
  //#region tabs logic
  // Tabs and sub-tabs
  tabs: string[] = [
    'Insight Elicitation',
    'Solidification',
    'Blueprinting',
    'Code Synthesis',
  ];
  blueprintingSubTabs: string[] = [
    'Requirement Summary',
    'Solution Overview',
    'Project Structure',
    'Data Flow',
    'Unit Testing',
    'Common Functionalities',
    'Database Scripts',
    'Functional Testing',
    'Integration Testing',
    'Documentation',
    'Code Review'
  ];

  alwaysSelectedTabs = ['Requirement Summary',
    'Solution Overview',
    'Project Structure',
    'Data Flow',
    'Common Functionalities',];

  selectedTabs: { [key: string]: boolean } = {};

  setActiveTab(tab: string) {
    this.activeTab = tab;
  }

  setActiveBlueprintingSubTab(subTab: string) {
    this.activeBlueprintingSubTab = subTab;
    if (this.activeBlueprintingSubTab === 'Project Structure') {
      this.inprojectstructure = true;
    }
    else {
      this.inprojectstructure = false;
    }
    if (this.activeBlueprintingSubTab === 'Documentation') {
      this.templateContent = this.templates['hld']
    }
  }

  showTemplate(type: string): void {
    this.activeTabDocumentation = type;
    this.templateContent = this.templates[type];
  }

  // showCodeReview(type: string): void {
  //   this.activeTabCodeReview = type;
  //   this.CodeReviewContent = this.CodeReviews.;
  // }



  //#endregion







  /* --------------------------------------------------
     Horizontal Slide Logic
  -------------------------------------------------- */

  //#region Horizontalslidestart 

  private mouseUpListener?: () => void;
  private mouseMoveListener?: (event: MouseEvent) => void;

  @HostListener('window:mousedown', ['$event'])
  onMouseDown(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const resizeZone = 10;

    // Only prevent default if we're actually starting a resize
    if (this.checkResizeHandle(target, event, resizeZone)) {
      event.preventDefault();
      event.stopPropagation();

      this.startResize();
    }
  }

  private startResize() {
    this.renderer.addClass(document.body, 'resizing');
    this.renderer.setStyle(document.body, 'cursor', 'col-resize');

    // Create and bind event listeners
    this.mouseMoveListener = (e: MouseEvent) => this.handleMouseMove(e);
    this.mouseUpListener = () => this.handleMouseUp();

    document.addEventListener('mousemove', this.mouseMoveListener, { passive: false });
    document.addEventListener('mouseup', this.mouseUpListener, { passive: false });

    // Add a small delay to ensure proper cleanup
    document.addEventListener('mouseleave', this.mouseUpListener, { passive: false });
  }

  private handleMouseMove(event: MouseEvent) {
    if (!this.resizingSection) return;

    event.preventDefault();
    event.stopPropagation();

    const container = document.querySelector('.code-synthesis-container') ||
      document.querySelector('.main-content');

    if (container) {
      const containerRect = container.getBoundingClientRect();
      const containerWidth = container.clientWidth;
      const mouseX = event.clientX - containerRect.left;
      const mousePercent = (mouseX / containerWidth) * 100;

      if (this.resizingSection === 'input' || this.resizingSection === 'output') {
        this.handleHorizontalResize('input', 'output', mousePercent);
      } else if (this.resizingSection === 'folder' || this.resizingSection === 'content') {
        this.handleHorizontalResize('folder', 'content', mousePercent);
      }
    }
  }

  private handleMouseUp() {
    if (this.resizingSection) {
      // Reset resize state
      this.resizingSection = null;

      // Remove visual indicators with a small delay to ensure proper cleanup
      setTimeout(() => {
        this.renderer.removeClass(document.body, 'resizing');
        this.renderer.removeStyle(document.body, 'cursor');
      }, 0);

      // Clean up event listeners
      this.removeEventListeners();
    }
  }

  private removeEventListeners() {
    if (this.mouseMoveListener) {
      document.removeEventListener('mousemove', this.mouseMoveListener);
      document.removeEventListener('mouseleave', this.mouseMoveListener);
      this.mouseMoveListener = undefined;
    }

    if (this.mouseUpListener) {
      document.removeEventListener('mouseup', this.mouseUpListener);
      document.removeEventListener('mouseleave', this.mouseUpListener);
      this.mouseUpListener = undefined;
    }
  }

  private checkResizeHandle(target: HTMLElement, event: MouseEvent, resizeZone: number): boolean {
    // Input section right edge
    const inputSection = target.closest('.input-section') as HTMLElement;
    if (inputSection) {
      const rect = inputSection.getBoundingClientRect();
      const relativeX = event.clientX - rect.left;
      if (relativeX > rect.width - resizeZone) {
        this.resizingSection = 'input';
        return true;
      }
    }

    // Output section left edge  
    const outputSection = target.closest('.output-section') as HTMLElement;
    if (outputSection) {
      const rect = outputSection.getBoundingClientRect();
      const relativeX = event.clientX - rect.left;
      if (relativeX < resizeZone) {
        this.resizingSection = 'output';
        return true;
      }
    }

    // Folder section right edge
    const folderSection = target.closest('.code-folder-structure-section') as HTMLElement;
    if (folderSection) {
      const rect = folderSection.getBoundingClientRect();
      const relativeX = event.clientX - rect.left;
      if (relativeX > rect.width - resizeZone) {
        this.resizingSection = 'folder';
        return true;
      }
    }

    // Content section left edge
    const contentSection = target.closest('.code-content-section') as HTMLElement;
    if (contentSection) {
      const rect = contentSection.getBoundingClientRect();
      const relativeX = event.clientX - rect.left;
      if (relativeX < resizeZone) {
        this.resizingSection = 'content';
        return true;
      }
    }

    return false;
  }

  private handleHorizontalResize(leftType: string, rightType: string, mousePercent: number) {
    const leftSection = this.getSectionElement(leftType);
    const rightSection = this.getSectionElement(rightType);

    if (!leftSection || !rightSection) return;

    let leftWidth: number;
    let rightWidth: number;

    if (this.resizingSection === leftType) {
      leftWidth = Math.max(this.minSectionWidth,
        Math.min(mousePercent, 100 - this.minSectionWidth));
      rightWidth = 100 - leftWidth;
    } else {
      rightWidth = Math.max(this.minSectionWidth,
        Math.min(100 - mousePercent, 100 - this.minSectionWidth));
      leftWidth = 100 - rightWidth;
    }

    leftSection.style.flex = `0 0 ${leftWidth}%`;
    rightSection.style.flex = `0 0 ${rightWidth}%`;
  }

  private getSectionElement(type: string): HTMLElement | null {
    switch (type) {
      case 'input': return document.querySelector('.input-section') as HTMLElement;
      case 'output': return document.querySelector('.output-section') as HTMLElement;
      case 'folder': return document.querySelector('.code-folder-structure-section') as HTMLElement;
      case 'content': return document.querySelector('.code-content-section') as HTMLElement;
      default: return null;
    }
  }
  //#endregion







  /* --------------------------------------------------
     Search and Dropdown functionality
  -------------------------------------------------- */
  //#region Search functionality
  // /* ========== WORKING GLOBAL LISTENERS (REPLACE PREVIOUS CODE) ========== */
  @HostListener('document:click', ['$event'])
  onGlobalClick(event: Event): void {
    const target = event.target as HTMLElement;

    // Don't close if clicking on search-related elements
    if (target.closest('.search-toggle') ||
      target.closest('.search-container') ||
      target.closest('.dropdown-menu') ||
      target.closest('.edit-actions')) {
      return;
    }

    // Close all open search boxes
    if (this.searchBoxVisible.input) {
      this.closeSearch('input');
    }
    if (this.searchBoxVisible.output) {
      this.closeSearch('output');
    }
    if (this.searchBoxVisible.tech) {
      this.closeSearch('tech');
    }
  }

  // @HostListener('document:keydown.escape', ['$event'])
  // onGlobalEscape(event: KeyboardEvent): void {
  //   let searchClosed = false;

  //   // Close all open search boxes
  //   if (this.searchBoxVisible.input) {
  //     this.closeSearch('input');
  //     searchClosed = true;
  //   }
  //   if (this.searchBoxVisible.output) {
  //     this.closeSearch('output');
  //     searchClosed = true;
  //   }
  //   if (this.searchBoxVisible.tech) {
  //     this.closeSearch('tech');
  //     searchClosed = true;
  //   }

  //   // Prevent default ESC behavior if we closed something
  //   if (searchClosed) {
  //     event.preventDefault();
  //   }
  // }


  /* ────── Edit Mode State ────── */
  editMode = { input: false, output: false, tech: false };
  editBackup = { input: '', output: '', tech: '' };

  /* ────── Search UI State ────── */
  searchBoxVisible = { input: false, output: false, tech: false };
  searchTerm = { input: '', output: '', tech: '' };
  currentMatch = { input: -1, output: -1, tech: -1 };
  isSearching = { input: false, output: false, tech: false };

  /* ────── Dropdown State ────── */
  dropdownVisible = { input: false, output: false, tech: false };
  selectedDropdownIndex = { input: -1, output: -1, tech: -1 };
  recentSearches = { input: [] as string[], output: [] as string[], tech: [] as string[] };
  filteredTerms = { input: [] as string[], output: [] as string[], tech: [] as string[] };
  private dropdownTimeouts = { input: null as any, output: null as any, tech: null as any };
  private isDropdownButtonClick = { input: false, output: false, tech: false };


  inputCommonTerms: string[] = INSIGHTCOMMONTERMS
    .split(/[\n,]+|\s{2,}/)
    .map(s => s.trim())
    .filter(Boolean);

  outputCommonTerms: string[] = INSIGHTCOMMONTERMS
    .split(/[\n,]+|\s{2,}/)
    .map(s => s.trim())
    .filter(Boolean);

  techCommonTerms = TECHNICALCOMMONTERMS;


  /* ────── DOM References ────── */
  @ViewChild('inputArea') inputArea!: ElementRef<HTMLDivElement>;
  @ViewChild('outputArea') outputArea!: ElementRef<HTMLDivElement>;
  @ViewChild('techArea') techArea!: ElementRef<HTMLDivElement>;
  @ViewChild('inputTextarea') inputTextarea?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('outputTextarea') outputTextarea?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('techTextarea') techTextarea?: ElementRef<HTMLTextAreaElement>;
  @ViewChild('inputFinder') inputFinder?: ElementRef<HTMLInputElement>;
  @ViewChild('outputFinder') outputFinder?: ElementRef<HTMLInputElement>;
  @ViewChild('techFinder') techFinder?: ElementRef<HTMLInputElement>;




  /* ========== CONTENT MANAGEMENT ========== */
private refreshContent(area: Area, forceRefresh: boolean = false): void {
  if (this.editMode[area]) return;
  
  const element = this.getDisplayElement(area);
  let text = '';
  
  if (area === 'output') {
    text = this.getActiveTabContent();
  } else if (area === 'tech') {
    text = this.getActiveTechTabContent();  // Add this line
  } else {
    text = this.getText(area);
  }
  
  if (!text.trim()) { 
    element!.innerHTML = ''; 
    return; 
  }

  if (element && (!this.isSearching[area] || forceRefresh)) {
    element.textContent = text;
  }
}

  private getDisplayElement(area: Area): HTMLElement | undefined {
    switch (area) {
      case 'input': return this.inputArea?.nativeElement;
      case 'output': return this.outputArea?.nativeElement;
      case 'tech': return this.techArea?.nativeElement;
    }
  }

  private getTextarea(area: Area): HTMLTextAreaElement | undefined {
    switch (area) {
      case 'input': return this.inputTextarea?.nativeElement;
      case 'output': return this.outputTextarea?.nativeElement;
      case 'tech': return this.techTextarea?.nativeElement;
    }
  }

  private getText(area: Area): string {
    switch (area) {
      case 'input': return this.inputText;
      case 'output': return this.outputText;
      case 'tech': return this.technicalRequirement;
    }
  }

  private setText(area: Area, text: string): void {
    switch (area) {
      case 'input': this.inputText = text; break;
      case 'output': this.outputText = text; break;
      case 'tech': this.technicalRequirement = text; break;
    }
  }

  /* ========== EDIT MODE MANAGEMENT ========== */
  /* ========== UPDATE EXISTING EDIT METHODS ========== */
enterEditMode(area: Area): void {
  // Backup current content
  if (area === 'output') {
    this.editBackup[area] = this.getActiveTabContent();
  } else if (area === 'tech') {
    this.editBackup[area] = this.getActiveTechTabContent();  // Uses separate variables
  } else {
    this.editBackup[area] = this.getText(area);
  }
  
  const display = this.getDisplayElement(area);
  const scrollPos = display?.scrollTop ?? 0;
  
  this.editMode[area] = true;
  
  setTimeout(() => {
    const ta = this.getTextarea(area);
    if (ta) {
      ta.focus();
      ta.setSelectionRange(0, 0);
      ta.scrollTop = scrollPos;
    }
  });
}

/* ========== UPDATE EXISTING cancelEdit METHOD ========== */
cancelEdit(area: Area): void {
  // Restore backup
  if (area === 'output') {
    if (this.activeOutputTab === 'output1') {
      this.outputText = this.editBackup[area];
    } else if(this.activeOutputTab === 'output2'){
      this.outputText2 = this.editBackup[area];
    }
    else if(this.activeOutputTab === 'output3'){
      this.outputText3 = this.editBackup[area];
    }
    else if(this.activeOutputTab === 'output4'){
      this.outputText4 = this.editBackup[area];
    } }
    else if (area === 'tech') {
    if (this.activeTechTab === 'requirement') {
      this.techRequirement1 = this.editBackup[area];
    }else if (this.activeTechTab === 'solution') {
      this.techRequirement2 = this.editBackup[area];
    }
     else {
      this.techRequirement3 = this.editBackup[area];
    }
  } else {
    this.setText(area, this.editBackup[area]);
  }
  
  const ta = this.getTextarea(area);
  const pos = ta?.scrollTop ?? 0;

  this.editMode[area] = false;
  this.editBackup[area] = '';

  requestAnimationFrame(() => {
    this.refreshContent(area);
    const box = this.getDisplayElement(area);
    if (box) { 
      this.restoreAndReveal(box, pos); 
    }
  });
}
  async saveChanges(area: Area): Promise<void> {
    const ta = this.getTextarea(area);
    const pos = ta?.scrollTop ?? 0;
    if (area === 'tech' && ta) {
    if (this.activeTechTab === 'requirement') {
      this.techRequirement1 = ta.value;
    } else if(this.activeTechTab === 'solution'){
      this.techRequirement2 = ta.value;
    }
    else {
      this.techRequirement3 = ta.value;
    }
  }
  if (area === 'output' && ta) {



    if (this.activeOutputTab === 'output1') {
      this.outputText = ta.value;
    } else if(this.activeOutputTab === 'output2'){
      this.outputText2 = ta.value;
    }
    else if(this.activeOutputTab === 'output3'){
      this.outputText3 = ta.value;
    }
    else if(this.activeOutputTab === 'output4'){
      this.outputText4 = ta.value;
    }
  }

  this.editMode[area] = false;
  this.editBackup[area] = '';

  requestAnimationFrame(() => {
    this.refreshContent(area);
    const box = this.getDisplayElement(area);
    if (box) { 
      this.restoreAndReveal(box, pos); 
    }
  });


    
    const projectname = this.extractProjectName(this.inputText);

    
    if (this.projectLifecycleData.projectID === 0 || this.projectLifecycleData.projectID == null) {
      this.projectLifecycleData.projectType = "Greenfield";
      if(projectname!=""){
        this.projectLifecycleData.projectName = projectname}
      await this.initializeAndSaveProject();
       
      // await this.syncToBackend('BRD Paste/Edit Completed');
     } 
      else {
        if(projectname !="" && projectname!=this.projectLifecycleData.projectName){
        this.projectLifecycleData.projectName = projectname
        await this.syncToBackend('BRD Paste/Edit Completed');
        }
      }
      
    
    
  }

  // cancelEdit(area: Area): void {
  //   this.apicallvariable = false;
  //   // Restore backup
  //   if (area === 'output') {
  //     if (this.activeOutputTab === 'output1') {
  //       this.outputText = this.editBackup[area];
  //     } else {
  //       this.outputText2 = this.editBackup[area];
  //     }
  //   } else {
  //     this.setText(area, this.editBackup[area]);
  //   }

  //   const ta = this.getTextarea(area);
  //   const pos = ta?.scrollTop ?? 0;

  //   this.editMode[area] = false;
  //   this.editBackup[area] = '';

  //   requestAnimationFrame(() => {
  //     this.refreshContent(area);
  //     const box = this.getDisplayElement(area);
  //     if (box) {
  //       this.restoreAndReveal(box, pos);
  //     }
  //   });
  // }

  private restoreAndReveal(box: HTMLElement, pos: number): void {
    box.classList.add('invisible');
    setTimeout(() => {
      box.scrollTop = pos;
      box.classList.remove('invisible');
    }, 50);
  }

  /* ========== SEARCH FUNCTIONALITY ========== */
  openSearch(area: Area): void {
    this.searchBoxVisible[area] = true;
    this.isSearching[area] = true;
    this.currentMatch[area] = -1;
    setTimeout(() => {
      this.getFinder(area)?.focus();
      this.showDropdown(area);
    });
  }

  closeSearch(area: Area): void {
    this.searchBoxVisible[area] = false;
    this.isSearching[area] = false;
    this.dropdownVisible[area] = false;
    this.searchTerm[area] = '';
    this.currentMatch[area] = -1;
    this.refreshContent(area, true);
  }

  clearSearch(area: Area): void {
    this.searchTerm[area] = '';
    this.currentMatch[area] = -1;
    this.dropdownVisible[area] = false;
    this.selectedDropdownIndex[area] = -1;
    this.filteredTerms[area] = [];

    const element = this.getDisplayElement(area);
    const text = this.getText(area);
    if (element) {
      element.innerHTML = this.escapeHtml(text);
    }

    setTimeout(() => this.getFinder(area)?.focus(), 0);
  }

  performSearch(area: Area): void {
    this.currentMatch[area] = -1;
    this.updateFilteredTerms(area);
    this.applyHighlights(area);
    this.selectedDropdownIndex[area] = -1;

    if (this.searchTerm[area].trim()) {
      setTimeout(() => this.jumpToMatch(area, true), 0);
    }
  }

  handleTextareaKeys(event: KeyboardEvent, area: Area): void {
    if (event.ctrlKey && event.key === 'f') {
      event.preventDefault();
      this.openSearch(area);
    }
  }

  handleSearchKeys(event: KeyboardEvent, area: Area): void {
    if (event.key === 'Enter') {
      event.preventDefault();

      if (this.selectedDropdownIndex[area] >= 0 && this.dropdownVisible[area]) {
        const selectedTerm = this.getSelectedDropdownTerm(area);
        if (selectedTerm) {
          this.selectSearchTerm(area, selectedTerm);
          return;
        }
      }

      this.jumpToMatch(area, !event.shiftKey);

      if (this.searchTerm[area].trim()) {
        this.addToRecentSearches(area, this.searchTerm[area].trim());
      }
    }
  }

  getMatchCount(area: Area): number {
    const element = this.getDisplayElement(area);
    return element?.querySelectorAll('mark').length || 0;
  }

  /* ========== DROPDOWN MANAGEMENT ========== */
  showDropdown(area: Area): void {
    if (this.dropdownTimeouts[area]) {
      clearTimeout(this.dropdownTimeouts[area]);
      this.dropdownTimeouts[area] = null;
    }

    this.dropdownVisible[area] = true;
    this.selectedDropdownIndex[area] = -1;
    this.updateFilteredTerms(area);
  }

  hideDropdown(area: Area): void {
    if (this.isDropdownButtonClick[area]) {
      this.isDropdownButtonClick[area] = false;
      return;
    }

    if (this.dropdownTimeouts[area]) {
      clearTimeout(this.dropdownTimeouts[area]);
    }

    this.dropdownTimeouts[area] = setTimeout(() => {
      this.dropdownVisible[area] = false;
      this.selectedDropdownIndex[area] = -1;
      this.dropdownTimeouts[area] = null;
    }, 200);
  }

  toggleDropdown(area: Area): void {
    this.isDropdownButtonClick[area] = true;

    if (this.dropdownTimeouts[area]) {
      clearTimeout(this.dropdownTimeouts[area]);
      this.dropdownTimeouts[area] = null;
    }

    if (this.dropdownVisible[area]) {
      this.dropdownVisible[area] = false;
      this.selectedDropdownIndex[area] = -1;
    } else {
      this.dropdownVisible[area] = true;
      this.selectedDropdownIndex[area] = -1;
      this.updateFilteredTerms(area);
      this.getFinder(area)?.focus();
    }

    setTimeout(() => {
      this.isDropdownButtonClick[area] = false;
    }, 200);
  }

  onDropdownMouseDown(area: Area): void {
    this.isDropdownButtonClick[area] = true;
  }

  onSearchFocus(area: Area): void {
    if (!this.isDropdownButtonClick[area]) {
      this.showDropdown(area);
    }
  }

  onSearchBlur(area: Area): void {
    if (!this.isDropdownButtonClick[area]) {
      this.hideDropdown(area);
    }
  }

  navigateDropdown(area: Area, direction: number): void {
    const totalItems = this.getTotalDropdownItems(area);
    if (totalItems === 0) return;

    const newIndex = this.selectedDropdownIndex[area] + direction;

    if (newIndex < 0) {
      this.selectedDropdownIndex[area] = totalItems - 1;
    } else if (newIndex >= totalItems) {
      this.selectedDropdownIndex[area] = 0;
    } else {
      this.selectedDropdownIndex[area] = newIndex;
    }
  }

  selectSearchTerm(area: Area, term: string): void {
    this.searchTerm[area] = term;
    this.addToRecentSearches(area, term);
    this.performSearch(area);

    setTimeout(() => {
      this.hideDropdown(area);
      this.getFinder(area)?.focus();
    }, 10);
  }

  getTotalDropdownItems(area: Area): number {
    return this.recentSearches[area].length +
      this.getFilteredCommonTerms(area).length +
      this.filteredTerms[area].length;
  }

  getFilteredCommonTerms(area: Area): string[] {
    const searchQuery = this.searchTerm[area].toLowerCase().trim();
    const commonTerms = area === 'input' ? this.inputCommonTerms
      : area === 'output' ? this.outputCommonTerms
        : this.techCommonTerms;

    if (!searchQuery || searchQuery.length < 2) {
      return commonTerms;
    }

    return commonTerms.filter(term =>
      term.toLowerCase().includes(searchQuery) &&
      term.toLowerCase() !== searchQuery
    );
  }

  /* ========== UPDATE EXISTING updateFilteredTerms METHOD ========== */
private updateFilteredTerms(area: Area): void {
  let text = '';
  
  if (area === 'output') {
    text = this.getActiveTabContent().toLowerCase();
  } else if (area === 'tech') {
    text = this.getActiveTechTabContent().toLowerCase();  // Add this line
  } else if (area === 'input') {
    text = this.inputText.toLowerCase();
  }
  
  const currentTerm = this.searchTerm[area].toLowerCase().trim();
  
  if (currentTerm.length < 2) {
    this.filteredTerms[area] = [];
    return;
  }

  const words = text.match(/\b\w{3,}\b/g) || [];
  const uniqueWords = [...new Set(words)];
  const filteredCommonTerms = this.getFilteredCommonTerms(area);
  
  this.filteredTerms[area] = uniqueWords
    .filter(word => 
      word.includes(currentTerm) && 
      word !== currentTerm &&
      !filteredCommonTerms.some(commonTerm => commonTerm.toLowerCase() === word)
    )
    .slice(0, 5);
}


  private getSelectedDropdownTerm(area: Area): string | null {
    const index = this.selectedDropdownIndex[area];
    let currentIndex = 0;

    if (index < this.recentSearches[area].length) {
      return this.recentSearches[area][index];
    }
    currentIndex += this.recentSearches[area].length;

    const filteredCommonTerms = this.getFilteredCommonTerms(area);
    if (index < currentIndex + filteredCommonTerms.length) {
      return filteredCommonTerms[index - currentIndex];
    }
    currentIndex += filteredCommonTerms.length;

    if (index < currentIndex + this.filteredTerms[area].length) {
      return this.filteredTerms[area][index - currentIndex];
    }

    return null;
  }

  highlightMatchingText(text: string, searchQuery: string): string {
    if (!searchQuery || searchQuery.length < 2) {
      return this.escapeHtml(text);
    }

    const escapedText = this.escapeHtml(text);
    const escapedQuery = this.escapeHtml(searchQuery);
    const regex = new RegExp(`(${this.escapeRegex(escapedQuery)})`, 'gi');

    return escapedText.replace(regex, '<strong class="match-highlight">$1</strong>');
  }

  /* ========== RECENT SEARCHES ========== */
  private addToRecentSearches(area: Area, term: string): void {
    if (!term.trim()) return;

    this.recentSearches[area] = this.recentSearches[area].filter(s => s !== term);
    this.recentSearches[area].unshift(term);
    this.recentSearches[area] = this.recentSearches[area].slice(0, 5);

    this.saveRecentSearches();
  }

  removeRecentSearch(area: Area, searchTerm: string, event: Event): void {
    event.stopPropagation();

    this.recentSearches[area] = this.recentSearches[area].filter(term => term !== searchTerm);
    this.saveRecentSearches();

    if (this.selectedDropdownIndex[area] >= this.recentSearches[area].length) {
      this.selectedDropdownIndex[area] = this.recentSearches[area].length - 1;
    }
  }

  clearAllRecentSearches(area: Area): void {
    this.recentSearches[area] = [];
    this.saveRecentSearches();
    this.selectedDropdownIndex[area] = -1;
  }

  private saveRecentSearches(): void {
    localStorage.setItem('brd-recent-searches', JSON.stringify(this.recentSearches));
  }

  private loadRecentSearches(): void {
    const saved = localStorage.getItem('brd-recent-searches');
    if (saved) {
      try {
        this.recentSearches = JSON.parse(saved);
      } catch (e) {
        this.recentSearches = { input: [], output: [], tech: [] };
      }
    }
  }

  /* ========== HIGHLIGHT LOGIC ========== */
private applyHighlights(area: Area): void {
  if (this.editMode[area]) return;
  
  let text = '';
  if (area === 'output') {
    text = this.getActiveTabContent();
  } else if (area === 'tech') {
    text = this.getActiveTechTabContent();  // Add this line
  } else {
    text = this.getText(area);
  }
  
  const term = this.searchTerm[area].trim();
  const element = this.getDisplayElement(area);
  
  if (!element) return;
  
  if (!term) {
    element.innerHTML = this.escapeHtml(text);
    return;
  }

  const escapedText = this.escapeHtml(text);
  const regex = new RegExp(this.escapeRegex(term), 'gi');
  const highlightedHtml = escapedText.replace(regex, match => `<mark>${match}</mark>`);
  
  element.innerHTML = highlightedHtml;
}

  private jumpToMatch(area: Area, forward: boolean): void {
    const element = this.getDisplayElement(area);
    const marks = element?.querySelectorAll<HTMLElement>('mark');

    if (!marks || marks.length === 0) return;

    if (forward) {
      this.currentMatch[area] = (this.currentMatch[area] + 1) % marks.length;
    } else {
      this.currentMatch[area] = this.currentMatch[area] <= 0
        ? marks.length - 1
        : this.currentMatch[area] - 1;
    }

    const targetMark = marks[this.currentMatch[area]];
    if (targetMark) {
      marks.forEach(mark => mark.classList.remove('current'));
      targetMark.classList.add('current');

      targetMark.scrollIntoView({ block: 'nearest', behavior: 'smooth' });

      setTimeout(() => this.getFinder(area)?.focus(), 0);
    }
  }

  /* ========== HELPER METHODS ========== */
  private getFinder(area: Area): HTMLInputElement | undefined {
    switch (area) {
      case 'input': return this.inputFinder?.nativeElement;
      case 'output': return this.outputFinder?.nativeElement;
      case 'tech': return this.techFinder?.nativeElement;
    }
  }

  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  private escapeRegex(text: string): string {
    return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }
  //#endregion




extractProjectName(text: string): string{
    const lines = text.split(/\r?\n/);

    // Regex to find the header anywhere in the line (case-insensitive)
    const headerRegex = /project\/applicationname\s*:?\s*(.*)/i;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const match = line.match(headerRegex);
      if (match) {
        // If group 1 has content, that's the project name on the same line
        if (match[1].trim()) {
          return match[1].trim();
        } 
        // Else, return next line if available
        else if (i + 1 < lines.length) {
          return lines[i + 1].trim();
        } else {
          return ''; // no project name found
        }
      }
    }

    return ''; // header line not found at all
  }


  /* --------------------------------------------------
     Insight Elicitation
  -------------------------------------------------- */

  //#region Insight Elicitation

  uploadBRD() {
   
    this.brdplaceholder = '';
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.txt'; // Only accept .txt files

    input.onchange = async (event) => {
      const target = event.target as HTMLInputElement;
      if (target.files && target.files.length > 0) {
        const file = target.files[0];

        // Strict validation for .txt files only
        if (!this.isValidTextFile(file)) {
          this.inputText = '';
          this.brdplaceholder = 'Error: Only .txt files are allowed!'
          // alert('Error: Only .txt files are allowed!');
          input.remove();
          return;
        }

        const reader = new FileReader();

        reader.onload = async (e) => {
          if (e.target) {
            const text = e.target.result as string;
            this.inputText = text;
            this.isBrownfield = false;
            this.isGreenfield = true;
            // Sync immediately after setting project name
            const projectname = this.extractProjectName(this.inputText); 
             if(projectname!=""){
              this.projectLifecycleData.projectName = this.extractProjectName(this.inputText);  
            }
             
            this.projectLifecycleData.projectType = "Greenfield";
            await this.initializeAndSaveProject();
           
            // await this.syncToBackend('Project Type Completed');
            // await this.syncToBackend('BRD File Selected');
            console.log('Text file loaded successfully:', file.name);
          } else {
            console.error('Error: Failed to read file content');
            alert('Error: Failed to read file content');
          }
          input.remove(); // Cleanup
        };

        reader.onerror = (error) => {
          console.error('Error reading file:', file.name, error);
          alert('Error: Failed to read the selected file');
          input.remove(); // Cleanup
        };

        reader.readAsText(file);
      }
    };

    input.click();
    input.remove();
  }
  private isValidTextFile(file: File): boolean {
    // Check file extension
    const fileName = file.name.toLowerCase();
    if (!fileName.endsWith('.txt')) {
      return false;
    }

    // Check MIME type for additional security
    const validMimeTypes = ['text/plain', 'text/txt'];
    if (!validMimeTypes.includes(file.type) && file.type !== '') {
      return false;
    }

    return true;
  }

  abortBRD() {
    // log

    this.abortApi$.next();
    this.isAnalyzingBRD = false;
      this.apicallvariable = false;
      this.abortedBRD = true;
      this.responseBRD = false;
        
    this.apiService.logFrontend(`[INFO] ${'abortbrd clicked'}`).subscribe();

    if (this.apiBRD) {

      this.apiBRD.unsubscribe();
      this.isAnalyzingBRD = false;
      this.apicallvariable = false;
      this.abortedBRD = true;
    }
  }
  showsuggestion: boolean = false;


  async SuggestBRD() {

    if (this.inputText) {
      this.showsuggestion = true;
      this.abortedBRD = false
      this.apiService.logFrontend(`[INFO] ${'AnalyseBRD started'}`).subscribe();

      this.settodefault(1)
      this.isAnalyzingBRD = true;
      this.apicallvariable = true;
      this.responseBRD = false;
      // this.projectLifecycleData.insightElicitationStatus = 'In Progress';
      // await this.syncToBackend('BRD Analysis Started');
      this.taskInput = "udo:";
      // this.taskInput="udo:";
      this.apiBRD = this.apiService.APIanalyzeBRD('', this.inputText, this.taskInput, 0, 0)
        .pipe(take(1))
        .subscribe(
          async (response) => {
            this.outputText = response;
            if (this.outputText.length > 0) {
              this.isAnalyzingBRD = false;
              this.apicallvariable = false;

              this.responseBRD = true;
              // Update status and sync immediately
              // this.projectLifecycleData.insightElicitationStatus = 'In Progress';
              // await this.syncToBackend('BRD Analysis Started');
            }
            // Stop the spinner after the response
          },
          (error) => {
            console.error('Error during BRD analysis:', error);
            // log
            this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();

            this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
            this.apicallvariable = false;
          }
        );
      this.taskInput = "";
    }
    else {
      this.outputPlaceholder = "Error : Require BRD";
    }
  }
  // showtree(){
  //   this.outputText = this.buildDashedStructure(this.inputText);
  // }


// buildDashedStructure(input: string): string {
//   if (!input) return '';

//   const lines = input.split('\n').map(line => line.trim()).filter(Boolean);
//   let output = '';
//   let i = 0;

//   while (i < lines.length) {
//     const headerMatch = lines[i].match(/^<header>\s*(.+)$/i);
//     if (headerMatch) {
//       const headerName = headerMatch[1].trim();
//       // Business Requirements logic
//       if (/^Business Requirements$/i.test(headerName)) {
//         output += '|-- Business Requirements\n';
//         i++;
//         // Gather multi-line content under Business Requirements (until next header or Module&SubModuleHierarchy)
//         while (
//           i < lines.length &&
//           !lines[i].match(/^<header>/i) &&
//           !lines[i].match(/^<header>\s*Module&SubModuleHierarchy/i)
//         ) {
//           output += '|   |-- ' + lines[i] + '\n';
//           i++;
//         }
//         // If next header is Module&SubModuleHierarchy, nest it and its content
//         if (
//           i < lines.length &&
//           lines[i].match(/^<header>\s*Module&SubModuleHierarchy/i)
//         ) {
//           output += '|   |-- Module&SubModuleHierarchy\n';
//           i++;
//           while (
//             i < lines.length &&
//             !lines[i].match(/^<header>/i)
//           ) {
//             output += '|   |   |-- ' + lines[i] + '\n';
//             i++;
//           }
//         }
//         continue;
//       } else if (/^Module&SubModuleHierarchy$/i.test(headerName)) {
//         // If Module&SubModuleHierarchy appears but not under Business Requirements, treat as top-level
//         output += '|-- Module&SubModuleHierarchy\n';
//         i++;
//         while (
//           i < lines.length &&
//           !lines[i].match(/^<header>/i)
//         ) {
//           output += '|   |-- ' + lines[i] + '\n';
//           i++;
//         }
//         continue;
//       } else {
//         // Top-level sections (including project name)
//         output += '|-- ' + headerName + '\n';
//         i++;
//         // Add section content until next header
//         while (
//           i < lines.length &&
//           !lines[i].match(/^<header>/i)
//         ) {
//           output += '|   |-- ' + lines[i] + '\n';
//           i++;
//         }
//       }
//     } else {
//       // Skip any non-header line (will be included above)
//       i++;
//     }
//   }

//   return output;
// }


step2text = '';
listofcontent: string[] = [];
listofcontenttree: string[] = [];



buildDashedStructureNoHeader(input: string): string {
  if (!input) return '';

  // List of known section headers to match, in input order
  const sectionHeaders = [
    'Project/ApplicationName: XYZ',
    'Business Objectives',
    'Business Requirements',
    'Module&SubModuleHierarchy',
    'Functional Requirements',
    'Technical Requirements',
    'Data Requirements',
    'Scope of Work',
    'Assumptions and Constraints',
    'Non-Functional Requirements',
    'Security Access Control',
    'Project Deliverables',
    'Project Milestones',
    'Success Criteria'
  ];

  const lines = input.split('\n').map(line => line.trim()).filter(Boolean);
  let output = '';
  let i = 0;
  let currentHeader = '';
  let isBusinessReq = false;

  while (i < lines.length) {
    // Check if the line is a header
    if (sectionHeaders.some(h => lines[i].toLowerCase() === h.toLowerCase())) {
      const headerName = lines[i];
      // Special handling for Business Requirements for nesting
      if (/^Business Requirements$/i.test(headerName)) {
        output += '|-- Business Requirements\n';
        isBusinessReq = true;
        i++;
        // Collect content under Business Requirements (until next header or Module&SubModuleHierarchy)
        while (
          i < lines.length &&
          !sectionHeaders.some(h => lines[i].toLowerCase() === h.toLowerCase()) &&
          (lines[i].toLowerCase() !== 'module&submodulehierarchy')
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          i++;
        }
        // If next header is Module&SubModuleHierarchy, nest and collect its content
        if (
          i < lines.length &&
          lines[i].toLowerCase() === 'module&submodulehierarchy'
        ) {
          output += '|   |-- Module&SubModuleHierarchy\n';
          i++;
          while (
            i < lines.length &&
            !sectionHeaders.some(h => lines[i].toLowerCase() === h.toLowerCase())
          ) {
            output += '|   |   |-- ' + lines[i] + '\n';
            i++;
          }
        }
        continue;
      } else if (/^Module&SubModuleHierarchy$/i.test(headerName)) {
        // If Module&SubModuleHierarchy appears at top-level
        output += '|-- Module&SubModuleHierarchy\n';
        i++;
        while (
          i < lines.length &&
          !sectionHeaders.some(h => lines[i].toLowerCase() === h.toLowerCase())
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          i++;
        }
        continue;
      } else {
        // Normal section
        output += '|-- ' + headerName + '\n';
        i++;
        while (
          i < lines.length &&
          !sectionHeaders.some(h => lines[i].toLowerCase() === h.toLowerCase())
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          i++;
        }
      }
    } else {
      // Skip lines not matching a header (should not happen in clean input)
      i++;
    }
  }
  return output;
}

buildDashedStructure(input: string): string {

  if (!input) return '';
this.listofcontent = [];
  const lines = input.split('\n').map(line => line.trim()).filter(Boolean);
  let output = '';
  let tempoutput = '';
  let i = 0;



  

  while (i < lines.length) {
    const headerMatch = lines[i].match(/^<header>\s*(.+)$/i);
    if (headerMatch) {
      const headerName = headerMatch[1].trim();
      // Business Requirements logic
      if (/^Business Requirements$/i.test(headerName)) {
        output += '|-- Business Requirements\n';
        tempoutput += '|-- Business Requirements\n';
        this.step2text += 'Business Requirements\n';
        i++;
        // Gather multi-line content under Business Requirements (until next header or Module&SubModuleHierarchy)
        while (
          i < lines.length &&
          !lines[i].match(/^<header>/i) &&
          !lines[i].match(/^<header>\s*Module&SubModuleHierarchy/i)
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          tempoutput += '|   |-- ' + lines[i] + '\n';
          this.step2text += lines[i] + '\n';
          i++;
        }
        // If next header is Module&SubModuleHierarchy, nest it and its content
        if (
          i < lines.length &&
          lines[i].match(/^<header>\s*Module&SubModuleHierarchy/i)
        ) {
          output += '|   |-- Module&SubModuleHierarchy\n';
          tempoutput += '|   |-- Module&SubModuleHierarchy\n';
          this.step2text += 'Module&SubModuleHierarchy\n';
          
          i++;
          while (
            i < lines.length &&
            !lines[i].match(/^<header>/i)
          ) {
            output += '|   |   |-- ' + lines[i] + '\n';
            tempoutput += '|   |   |-- ' + lines[i] + '\n';
          this.step2text += lines[i] + '\n';

            i++;
          }
        }
        continue;
      } else if (/^Module&SubModuleHierarchy$/i.test(headerName)) {
        // If Module&SubModuleHierarchy appears but not under Business Requirements, treat as top-level
        output += '|-- Module&SubModuleHierarchy\n';
        tempoutput += '|-- Module&SubModuleHierarchy\n';
        this.step2text += 'Module&SubModuleHierarchy\n';
        i++;
        while (
          i < lines.length &&
          !lines[i].match(/^<header>/i)
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          tempoutput += '|   |-- ' + lines[i] + '\n';
        this.step2text += lines[i] + '\n';
          i++;
        }
        continue;
      } else {
        // Top-level sections (including project name)
        output += '|-- ' + headerName + '\n';
        tempoutput += '|-- ' + headerName + '\n';
        this.step2text += headerName + '\n';

        i++;
        // Add section content until next header
        while (
          i < lines.length &&
          !lines[i].match(/^<header>/i)
        ) {
          output += '|   |-- ' + lines[i] + '\n';
          tempoutput += '|   |-- ' + lines[i] + '\n';
        this.step2text += lines[i] + '\n';

          i++;
        }
      }
    } else {
      // Skip any non-header line (will be included above)
      i++;
    }
    console.log(this.step2text)
    this.listofcontent.push(this.step2text)
    console.log(this.listofcontent);
    this.step2text = '';
    this.listofcontenttree.push(tempoutput)
    console.log(this.listofcontenttree);
    tempoutput = '';
    console.log("---------------------------")


  }

  return output;
}

private abortApi$ = new Subject<void>();
async analyzeBRD() {
    // this.projectLifecycleData.insightElicitationStatus = 'In Progress';
    // await this.syncToBackend('BRD Analysis Started');
    this.settodefault(1)
    if (this.insidestep1) {
 
      if (this.inputText) {
        // log
        this.apiService.logFrontend(`[INFO] ${'AnalyseBRD started'}`).subscribe();
        this.outputPlaceholder = ''
 
        // validate template
        if (!this.isVisible) {
          this.outputText = '';
          this.outputPlaceholder = 'Starting validation …';
          this.cd.detectChanges();
 
          // Live placeholder feedback
          const haystack = this.normalizeText(this.inputText);
          const feed: string[] = [];
 
          for (let i = 0; i < this.requiredSections.length; i++) {
            const sec = this.requiredSections[i];
            const isMandatory = this.mandatoryFlags[i];
            feed.push(`🔎 Checking “${sec}”${isMandatory ? ' (mandatory)' : ' (optional)'}…`);
            this.outputPlaceholder = feed.join('\n');
            this.cd.detectChanges();
            await this.sleep(120);
 
            const ok = haystack.includes(this.normalizeSection(sec));
            const icon = ok ? '✅' : (isMandatory ? '❌' : '⚠️');
            const typeLabel = isMandatory ? 'mandatory' : 'optional';
            const resultLabel = ok ? 'found' : (isMandatory ? 'MISSING' : 'missing');
 
            feed[feed.length - 1] =
              `${icon} ${sec} (${typeLabel}) ${resultLabel}`;
            this.outputPlaceholder = feed.join('\n');
            this.cd.detectChanges();
            await this.sleep(120);
          }
 
          // Final result
          const v = this.validateBRD(this.inputText);
          const mandTotal = v.foundMandatory.length + v.missingMandatory.length;
          const optTotal = v.foundOptional.length + v.missingOptional.length;
 
          const mandPct = mandTotal ? (v.foundMandatory.length / mandTotal * 100).toFixed(1) : '0';
          const optPct = optTotal ? (v.foundOptional.length / optTotal * 100).toFixed(1) : '0';
 
          feed.push(
            `\nMandatory sections : ${v.foundMandatory.length}/${mandTotal}  (${mandPct}%)`,
            `Optional sections  : ${v.foundOptional.length}/${optTotal}   (${optPct}%)`,
            `Overall score      : ${v.score}%`
          );
          this.outputPlaceholder = feed.join('\n');
          this.cd.detectChanges();
 
          if (!v.pass) {
            feed.push('\n❌ Validation failed – missing mandatory sections:',
              ...v.missingMandatory.map(m => `• ${m}`));
            this.outputPlaceholder = feed.join('\n');
            this.isAnalyzingBRD = false;
            this.cd.detectChanges();
            return; // No API call
          }
 
          // Success: call API
          feed.push('\n✅ All mandatory sections present. Sending to analysis …');
          this.outputPlaceholder = feed.join('\n');
          this.cd.detectChanges();
          this.outputText = this.buildDashedStructure(this.inputText);
          this.switchOutputTab('output2');
 
          // this.responseBRD = true;
 
 
 
 
             this.isAnalyzingBRD = true;
    this.apicallvariable = true;
    this.responseBRD = false;
    for (const item of this.listofcontent) {
      // console.log(`processing: ${item}`)
      const processingitem = item.trim().split('\n');
      this.outputText2 += processingitem[0] + "\n\n";
      if(item.trim().startsWith("/Project/i") ||  processingitem.length == 1){
        continue;
      }
     
    // Mark start of analysis
   
    this.taskInput = " ";
 
 
    const result = item.split('\n').slice(1).join('\n');
    console.log(`processing:${processingitem[0]} - ${result}`)
 
    try {
      // Await the response—loop will wait here
      const response = await firstValueFrom(
        this.apiService.APIanalyzeBRD(processingitem[0], result, this.taskInput, 1, this.projectLifecycleData.projectID)
          .pipe(takeUntil(this.abortApi$))
      );
      this.outputText2 += response + '\n\n' ;
      // this.responseBRD = !!this.outputText2;
    } catch (error) {
      // this.outputText2 = "❌ API error: Http failure response";
       this.isAnalyzingBRD = false;
    this.apicallvariable = false;
 
      console.error('Error during BRD analysis:', error);
      this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
      // Stop loop if aborted
    break;
    } finally {
    // Set responseBRD only if at least one response succeeded
    // this.responseBRD = !!this.outputText2;
    }
   
    // this.responseBRD = true;
  }
 
    this.outputText2 +=  "greenfield/brownfield:green";
 
    this.switchOutputTab('output3');
 
    try {
      // Await the response—loop will wait here
      const response = await firstValueFrom(
        this.apiService.APIanalyzeBRD('', this.outputText2, this.taskInput, 2,this.projectLifecycleData.projectID)
          .pipe(takeUntil(this.abortApi$))
      );
      this.outputText3 = response;
                console.log("response3: ", this.outputText3);
               
    } catch (error) {
       this.outputText2 = "❌ API error: Http failure response";
       this.isAnalyzingBRD = false;
    this.apicallvariable = false;
 
      console.error('Error during BRD analysis:', error);
      this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
      // Stop loop if aborted
 
    } finally {
    // Set responseBRD only if at least one response succeeded
    // this.responseBRD = !!this.outputText2;
    }
 
            //  this.apiBRD = await this.apiService.APIanalyzeBRD('', this.outputText2, this.taskInput, 2,this.projectLifecycleData.projectID)
            // .pipe(take(1))
            // .subscribe(
            //   async (response) => {
            //     this.outputText3 = response;
            //     console.log("response3: ", this.outputText3);
            //     if (this.outputText3.length > 0) {
                 
            //     }
               
            //   },
            //   (error) => {
            //     // this.outputText3 = `❌ API error: Http failure response`;
 
            //     console.error('Error during BRD analysis:', error);
            //     // log
            //     this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
            //     this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
            //     this.apicallvariable = false;
            //   }
            // );
 
    this.switchOutputTab('output4');
 
 
    try {
      // Await the response—loop will wait here
      const response = await firstValueFrom(
        this.apiService.APIanalyzeBRD('', this.outputText3, this.taskInput, 3,this.projectLifecycleData.projectID)
          .pipe(takeUntil(this.abortApi$))
      );
      this.outputText4 = response;
                console.log("response4: ", this.outputText4);
                if (this.outputText4.length > 0) {
                  this.isAnalyzingBRD = false;
                  this.apicallvariable = false;
                  this.responseBRD = true;
                  // this.projectLifecycleData.insightElicitationStatus = 'Completed';
                  // await this.syncToBackend('BRD Analysis Completed');
                }
               
    } catch (error) {
      // this.outputText2 = "❌ API error: Http failure response";
       this.isAnalyzingBRD = false;
    this.apicallvariable = false;
 
      console.error('Error during BRD analysis:', error);
      this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
      // Stop loop if aborted
 
    } finally {
    // Set responseBRD only if at least one response succeeded
    // this.responseBRD = !!this.outputText2;
    }
 
    // this.apiBRD = await this.apiService.APIanalyzeBRD('', this.outputText3, this.taskInput, 3,this.projectLifecycleData.projectID)
    //         .pipe(take(1))
    //         .subscribe(
    //           async (response) => {
    //             this.outputText4 = response;
    //             console.log("response4: ", this.outputText4);
    //             if (this.outputText4.length > 0) {
    //               this.isAnalyzingBRD = false;
    //               this.apicallvariable = false;
    //               this.responseBRD = true;
    //               // this.projectLifecycleData.insightElicitationStatus = 'Completed';
    //               // await this.syncToBackend('BRD Analysis Completed');
    //             }
    //             // Stop the spinner after the response
    //           },
    //           (error) => {
    //             // this.outputText3 = `❌ API error: Http failure response`;
 
    //             console.error('Error during BRD analysis:', error);
    //             // log
    //             this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
    //             this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
    //             this.apicallvariable = false;
    //           }
    //         );
 



     const delimiters = ['Solution Overview', 'Solution Structure', 'Requirementsummary'];
      const result = this.splitText(this.outputText4, delimiters);
      this.techsolutionstructure = result[1];
      console.log(result);
          return;
       }
 
        this.isAnalyzingBRD = true;
        this.apicallvariable = true;
        this.responseBRD = false;
        if (!this.taskInput) {
          this.taskInput = " ";
          this.apiBRD = this.apiService.APIanalyzeBRD('', this.inputText, this.taskInput, 1, this.projectLifecycleData.projectID)
            .pipe(take(1))
            .subscribe(
              (response) => {
                this.outputText = response;
                //validate template
                this.outputPlaceholder = '';
                this.cd.detectChanges();
                if (this.outputText.length > 0) {
                  this.isAnalyzingBRD = false;
                  this.apicallvariable = false;
                  this.responseBRD = true;
                 
                }
                // Stop the spinner after the response
              },
              (error) => {
                this.outputText = `❌ API error: Http failure response`;
                this.outputPlaceholder = '';
                console.error('Error during BRD analysis:', error);
                // log
                this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
                this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
                this.apicallvariable = false;
 
                this.cd.detectChanges();
              }
            );
        }
        else {
          this.apiBRD = this.apiService.APIanalyzeBRD('', this.inputText, this.taskInput, 0,this.projectLifecycleData.projectID)
            .pipe(take(1))
            .subscribe(
              (response) => {
                this.outputText = response;
                if (this.outputText.length > 0) {
                  this.isAnalyzingBRD = false;
                  this.apicallvariable = false;
                  this.responseBRD = true;
                }
                // Stop the spinner after the response
              },
              (error) => {
                console.error('Error during BRD analysis:', error);
                // log
                this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
                this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
                this.apicallvariable = false;
              }
            );
        }
      }
      else {
        this.outputPlaceholder = "Error : Require BRD"
      }
    }
    else if (this.insidestep2) {
      if (this.outputText) {
        // log
        this.apiService.logFrontend(`[INFO] ${'AnalyseBRD started'}`).subscribe();
        this.isAnalyzingBRD = true;
        this.apicallvariable = true;
        this.responseBRD = false;
        if (!this.taskInput) {
          this.taskInput = " ";
          this.apiBRD = this.apiService.APIanalyzeBRD('', this.outputText2, this.taskInput, 2,this.projectLifecycleData.projectID)
            .pipe(take(1))
            .subscribe(
              async (response) => {
                this.outputText2 = response;
 
                if (this.outputText2.length > 0) {
                  this.isAnalyzingBRD = false;
                  this.apicallvariable = false;
                  this.responseBRD = true;
                  // this.projectLifecycleData.insightElicitationStatus = 'Completed';
                  // await this.syncToBackend('BRD Analysis Completed');
                }
                // Stop the spinner after the response
              },
              (error) => {
                this.outputText2 = `❌ API error: Http failure response`;
 
                console.error('Error during BRD analysis:', error);
                // log
                this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
                this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
                this.apicallvariable = false;
              }
            );
        }
        else {
          this.apiBRD = this.apiService.APIanalyzeBRD('', this.outputText, this.taskInput, 2,this.projectLifecycleData.projectID)
            .pipe(take(1))
            .subscribe(
              async (response) => {
                this.outputText2 = response;
                if (this.outputText2.length > 0) {
                  this.isAnalyzingBRD = false;
                  this.apicallvariable = false;
                  this.responseBRD = true;
                  this.projectLifecycleData.insightElicitationStatus = 'Completed';
                  await this.syncToBackend('BRD Analysis Completed');
                }
                // Stop the spinner after the response
              },
              (error) => {
                console.error('Error during BRD analysis:', error);
                // log
                this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
 
                this.isAnalyzingBRD = false; // Stop the spinner even if there's an error
                this.apicallvariable = false;
              }
            );
        }
      }
      else {
        this.outputPlaceholder = "Error : Require BRD"
      }
    }
  }
 












  //#endregion







  /* --------------------------------------------------
     Solidification Section
  -------------------------------------------------- */

  //#region Solidification



splitText(text: string, delimiters: string[]): string[] {
  if (!text || !delimiters || delimiters.length === 0) {
    return [text];
  }

  // Escape special regex characters
  const escapeRegex = (str: string): string => {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  };

  // Create regex pattern from delimiters (case insensitive)
  const delimiterPattern = delimiters
    .map(delimiter => escapeRegex(delimiter.trim()))
    .join('|');
  
  const regex = new RegExp(`(${delimiterPattern})`, 'gi');

  // Split text while keeping delimiters
  const parts = text.split(regex);
  
  // Process the parts to remove delimiters and empty strings
  const result: string[] = [];
  
  for (let i = 0; i < parts.length; i++) {
    const part = parts[i]?.trim();
    
    if (!part) continue;
    
    // Check if this part is a delimiter (case insensitive)
    const isDelimiter = delimiters.some(delimiter => 
      delimiter.toLowerCase() === part.toLowerCase()
    );
    
    if (!isDelimiter) {
      result.push(part);
    }
  }

  return result.filter(part => part.length > 0);
}

techsolutionstructure = "";

parseIndentedStructure(text: string): string {
  if (!text) return "";

  const lines = text.split('\n');
  let result: string[] = [];

  for (let line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;

    // Count leading whitespace for indentation level
    const indentMatch = line.match(/^\s*/);
    const indentSize = indentMatch ? indentMatch[0].length : 0;
    
    // Calculate hierarchy level (assuming 2 spaces or 1 tab per level)
    const level = Math.floor(indentSize / 2) || (indentSize > 0 ? 1 : 0);
    
    // Create dashed prefix based on hierarchy level
    const prefix = '|--'.repeat(level);
    
    // Add the structured line
    result.push(`${prefix}${trimmed}`);
  }

  return result.join('\n');

}


// parseSolutionStructure(text: string): string {
//   if (!text) return "";

//   const lines = text.split('\n');
//   let result: string[] = [];
//   let currentLevel = 0;

//   // Define case insensitive keywords and their levels
//   const rootKeys = ["solution structure:", "solutionname:", "rootfolder:", "project:", "projectname:", "paths:"];
//   const projectPathKey = "projectpath:";
//   const filesKey = "files:";
//   const filenameKey = "filename:";

//   for (const line of lines) {
//     const trimmed = line.trim();
//     if (!trimmed) continue;

//     const lower = trimmed.toLowerCase();
//     let prefix = '';
//     let level = 0;

//     // Determine hierarchy level based on keywords
//     if (rootKeys.some(k => lower.startsWith(k))) {
//       level = 0; // Root level: Solution Structure, SolutionName, RootFolder, Project, ProjectName, Paths
//       prefix = '|-- ';
//     } else if (lower.startsWith(projectPathKey)) {
//       level = 1; // Level 1: ProjectPath under Paths
//       prefix = '    ├── ';
//     } else if (lower.startsWith(filesKey)) {
//       level = 2; // Level 2: Files under ProjectPath
//       prefix = '    │   ├── ';
//     } else if (lower.startsWith(filenameKey)) {
//       level = 3; // Level 3: Filename under Files
//       prefix = '    │   │   ├── ';
//     } else {
//       // Content under filename
//       level = Math.max(currentLevel, 4);
//       prefix = '    │   │   │   ';
//     }

//     currentLevel = level;
//     result.push(prefix + trimmed);
//   }

//   return result.join('\n');
// }

parseNestedSolutionStructure(text: string): string {
  if (!text) return "";

  const lines = text.split('\n');
  let result: string[] = [];
  let currentLevel = 0;

  const rootKeys = ["solution structure:", "solutionname:", "rootfolder:", "project:", "projectname:", "paths:"];
  const projectPathKey = "projectpath:";
  const filesKey = "files:";
  const filenameKey = "filename:";

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    
    const lower = trimmed.toLowerCase();
    let prefix = '';

    if (rootKeys.some(k => lower.startsWith(k))) {
      currentLevel = 0;
      prefix = '|-- ';
    } else if (lower.startsWith(projectPathKey)) {
      currentLevel = 1;
      prefix = '    ├── ';
    } else if (lower.startsWith(filesKey)) {
      currentLevel = 2;
      prefix = '    │   ├── ';
    } else if (lower.startsWith(filenameKey)) {
      currentLevel = 3;
      prefix = '    │   │   ├── ';
    } else {
      // Content under filename gets proper tree branches
      currentLevel = 4;
      prefix = '    │   │   │      ├── ';
    }

    result.push(prefix + trimmed);
  }

  return result.join('\n');
}



 parseDocument(text: string) {
  const result: { solutionOverview: string; solutionDetails: string; requirements: string } = {
    solutionOverview: '',
    solutionDetails: '',
    requirements: ''
  };
 
  // Extract Solution Overview block
  const overviewMatch = text.match(/Solution Overview:([\s\S]*?)(?=Solution Structure:)/);
  if (overviewMatch) {
    result.solutionOverview = overviewMatch[1].trim();
  }
 
  // Extract Solution Details block (between Solution Overview and RequirementSummary)
  const detailsMatch = text.match(/Solution Overview:[\s\S]*?Solution Structure:([\s\S]*?)(?=RequirementSummary:)/);
  if (detailsMatch) {
    result.solutionDetails = detailsMatch[1].trim();
  }
 
  // Extract Requirements block (after RequirementSummary)
  const reqMatch = text.match(/RequirementSummary:([\s\S]*)$/);
  if (reqMatch) {
    result.requirements = reqMatch[1].trim();
  }
 
  return result;
}
 

splitSolutionTextExact(text: string): string[] {
  if (!text) return [];
 
  const lines = text.split('\n').map(line => line.trim()).filter(line => line);
  const result: string[] = [];
 
  const solutionNameRegex = /^solutionname:/i;
  const rootFolderRegex = /^rootfolder:/i;
  const projectPathRegex = /^projectpath:/i;
  
  // Lines to ignore (case insensitive)
  const ignoreRegexes = [
    /^project\s*:?\s*$/i,  // Match "Project:"
    /^projectname:/i,
    /^paths:/i,
    /^files:/i           // Also ignore "files:" lines
  ];
 
  let solutionNameLine = '';
  let rootFolderLine = '';
  let currentSection: string[] = [];
 
  // First pass: extract SolutionName and RootFolder
  for (const line of lines) {
    if (solutionNameRegex.test(line)) {
      solutionNameLine = line;
    } else if (rootFolderRegex.test(line)) {
      rootFolderLine = line;
    }
  }
 
  // Add SolutionName and RootFolder to index 0 if both found
  if (solutionNameLine && rootFolderLine) {
    result.push(`${solutionNameLine}\n${rootFolderLine}`);
  }
 
  // Second pass: split by ProjectPath, ignoring certain lines
  for (const line of lines) {
    // Skip SolutionName and RootFolder (already in index 0)
    if (solutionNameRegex.test(line) || rootFolderRegex.test(line)) {
      continue;
    }
 
    // Ignore Project:, ProjectName:, Paths:, and Files: lines
    if (ignoreRegexes.some(regex => regex.test(line))) {
      continue;
    }
 
    if (projectPathRegex.test(line)) {
      // Push previous section if not empty
      if (currentSection.length > 0) {
        result.push(currentSection.join('\n'));
        currentSection = [];
      }
      currentSection.push(line);
    } else {
      currentSection.push(line);
    }
  }
 
  // Push the last section
  if (currentSection.length > 0) {
    result.push(currentSection.join('\n'));
  }
 
  return result.filter(section => section.trim().length > 0);
}
 
 solidifyresponse = '';
private abortSOL$ = new Subject<void>();
  async solidify() {
    this.SOLfeedback = '';
    this.settodefault(2)
    if (this.outputText2) {



      const parsed = this.parseDocument(this.outputText2);
      console.log("Solution Details:", parsed.solutionDetails);
      const delimiters = ['Solution Overview', 'Solution Structure', 'Requirementsummary'];
      const result = this.splitText(this.outputText4, delimiters);
      this.techsolutionstructure = parsed.solutionDetails;
      // console.log(result);

      this.techRequirement1 = this.parseNestedSolutionStructure(this.techsolutionstructure);
      const treecontent = this.splitSolutionTextExact(this.techsolutionstructure);
      console.log(treecontent)
      // console.log("temp",temp);
      // this.techRequirement2 = treecontent.join('\n\n\n\n')


    let fileSections = this.techsolutionstructure.split('filename:');
    let currentContent = fileSections[0]; // Content before first filename (Solution Structure etc.)

    // Update this.techrequirement2 with initial content (preserves original indentation)
    this.switchTechTab('solution')
    this.techRequirement2 = currentContent;


      this.isAnalyzingSOL = true;
      this.apicallvariable = true;
      this.responseSOL = false;
      const treeheader = treecontent.shift();


  for (let i = 0; i< treecontent.length;i++) {
            const item = treecontent[i];
            const treesection = `${treeheader} \n ${item}`
            console.log(treesection)
            // console.log(input)
    try {
      // Await the response—loop will wait here
      const input = `solution overview: \n${parsed.solutionOverview} \n\nsolution structure: \n ${treesection} \n\nrequirementsummary: \n ${parsed.requirements}`;
            console.log(input)
      const response = await firstValueFrom(
        this.apiService.Solidify(input,this.projectLifecycleData.projectID, 2)
          .pipe(takeUntil(this.abortSOL$))
      );
        this.solidifyresponse = response  ;

      // this.techRequirement2 += response + '\n\n' ;
      // this.responseBRD = !!this.outputText2;
    } catch (error) {
      // this.outputText2 = "❌ API error: Http failure response";
       this.isAnalyzingSOL = false;
    this.apicallvariable = false;

 
      console.error('Error during SOL analysis:', error);
      this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
      // Stop loop if aborted
    break;
    } finally {
    // Set responseBRD only if at least one response succeeded
    // this.responseBRD = !!this.outputText2;
    }

       if (i + 1 < fileSections.length) {
        const section = fileSections[i + 1];
        
        // Find implementationdetails and methods positions
        const implementationStart = section.indexOf('implementationdetails:');
        const methodsStart = section.indexOf('methods:', implementationStart);

        if (implementationStart !== -1 && methodsStart !== -1) {
          // Extract parts: before implementationdetails, and from methods onwards
          const beforeImpl = section.substring(0, implementationStart);
          const afterMethods = section.substring(methodsStart);

          // Replace implementation details for this specific file section
          // This preserves the original indentation from beforeImpl and afterMethods
          fileSections[i + 1] = beforeImpl + this.solidifyresponse + '\n' + afterMethods;
        }

        // Join progressively and UPDATE this.techrequirement2 at each iteration
        // The 'filename:' delimiter and all original spacing/indentation is preserved
        currentContent += 'filename:' + fileSections[i + 1];
        this.techRequirement2 = currentContent;
        
        console.log(`Iteration ${i + 1}:`, this.techRequirement2);
      }
    }

     this.switchTechTab('solution3')
   
    const input = `solution overview: \n${parsed.solutionOverview} \n\nsolution structure: \n ${this.techRequirement2} \n\nrequirementsummary: \n ${parsed.requirements}`;
   
    try {
      const response = await firstValueFrom(
        this.apiService.Solidify(input, this.projectLifecycleData.projectID, 3)
          .pipe(takeUntil(this.abortSOL$))
      );
        this.techRequirement3 = response  ;
 
      // this.techRequirement2 += response + '\n\n' ;
      // this.responseBRD = !!this.outputText2;
    } catch (error) {
      // this.outputText2 = "❌ API error: Http failure response";
       this.isAnalyzingSOL = false;
      this.apicallvariable = false;
 
 
      console.error('Error during SOL analysis:', error);
      this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
      // Stop loop if aborted
    } finally {
    // Set responseBRD only if at least one response succeeded
    // this.responseBRD = !!this.outputText2;
    }
    
   
    // this.responseBRD = true;
  

  this.isAnalyzingSOL = false;
    this.apicallvariable = false;
    this.responseSOL = true;
    if(!this.abortedSOL){
    this.SOLfeedback = 'Response Received'}














    //   this.abortedSOL = false
    //   // log
    //   this.apiService.logFrontend(`[INFO] ${'solidify started'}`).subscribe();


    //   // this.technicalRequirement = this.outputText;
    //   this.isAnalyzingSOL = true;
    //   this.apicallvariable = true;
    //   this.responseSOL = false;
    //   //console.log(this.outputText,"this.outputtext");
    //   this.apiSOL = this.apiService.Solidify(this.outputText2,this.projectLifecycleData.projectID).subscribe(
    //     async (response) => {
    //       this.technicalRequirement = response;
    //       if (this.technicalRequirement.length > 0) {
    //         this.isAnalyzingSOL = false;
    //         this.apicallvariable = false;
    //         this.responseSOL = true;
    //         // Update status and sync
    //         // this.projectLifecycleData.solidificationStatus = 'Completed';
    //         // await this.syncToBackend('Solidification Completed');
    //         this.SOLfeedback = 'RESPONSE RECEIVED';
    //       }
    //       // Stop the spinner after the response
    //     },
    //     (error) => {
    //       console.error('Error during Solidify:', error);

    //       //log
    //       this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();

    //       this.isAnalyzingSOL = false; // Stop the spinner even if there's an error
    //       this.apicallvariable = false;

    //     }
    //   );
    // }
    // else {
    //   this.SOLfeedback = 'OPTIMIZED BRD WITH TRD NOT FOUND'
    // }

}





   
  }

  abortSOL() {
    this.abortSOL$.next();
this.isAnalyzingSOL = false;
      this.apicallvariable = false;
      this.abortedSOL = true;
      this.SOLfeedback = 'You stopped this response'
    if (this.apiSOL) {
      this.apiSOL.unsubscribe();
      this.isAnalyzingSOL = false;
      this.apicallvariable = false;
      this.abortedSOL = true;
      this.SOLfeedback = 'You stopped this response'
    }
  }
  //#endregion







  /* --------------------------------------------------
     Blueprinting Section
  -------------------------------------------------- */
  //#region Blueprinting


  abortBLU() {
    // this.abortedBLU = false;
    if (this.apiBLU) {
      this.apiBLU.unsubscribe();
      this.isAnalyzingBLU = false;
      this.apicallvariable = false;
      this.abortedBLU = true;
      this.BLUfeedback = 'You stopped this response';
    }
  }



  inputBLU = "";

  blueprinting() {
    this.BLUfeedback = '';
    this.settodefault(3)
   
    if (this.technicalRequirement || this.isBrownfield) {
       const selected = Object.keys(this.selectedTabs).filter(tab => this.selectedTabs[tab]);
      console.log(selected);
    if(selected.length === 0){
      this.BLUfeedback = "Please Select Any Tab"
      return;
    }
      if (this.isBrownfield) {
        this.inputBLU = this.solutionOverview;
        console.log("inputBLue: ", this.inputBLU);
      }
      else if (this.isGreenfield) {
        this.inputBLU = this.technicalRequirement;
        console.log("inputBLue: ", this.inputBLU);
      }
      this.abortedBLU = false
      // log
      this.apiService.logFrontend(`[INFO] ${'blueprinting started'}`).subscribe();

      this.isAnalyzingBLU = true;
      this.apicallvariable = true;
      this.responseBLU = false;

      

      this.apiBLU = this.apiService.Blueprinting(this.inputBLU,
        selected,this.projectLifecycleData.projectID).subscribe(
          async (response: { [key: string]: string }) => {
            if (this.selectedTabs['Solution Overview']) {
              this.solutionOverview = response['solutionOverview'];
            }
            if (this.selectedTabs['Data Flow']) {
              this.dataFlow = response['dataFlow'];
            }
            if (this.selectedTabs['Common Functionalities']) {
              this.commonFunctionalities = response['commonFunctionalities'];
            }
            if (this.selectedTabs['Project Structure'] && this.isGreenfield) {
              this.projectStructure = response['projectStructure'];
              this.fetchFolderStructure(this.projectStructure);
              this.projectStructureDescription = this.projectStructure;
            }
            if (this.selectedTabs['Requirement Summary'] && this.isGreenfield) {
              this.requirementSummary = response['requirementSummary'];
            }
            if (this.selectedTabs['Unit Testing']) {
              this.unitTesting = response['unitTesting'];
            }
            if (this.selectedTabs['Functional Testing']) {
              this.FunctionalTesting = response['FunctionalTesting'];
            }
            if (this.selectedTabs['Integration Testing']) {
              this.IntegrationTesting = response['IntegrationTesting'];
            }
            if (this.selectedTabs['Database Scripts']) {
              this.databaseScripts = response['databaseScripts'];
            }

            //this.databaseScripts = this.cleanUnitTestTextAdvanced(this.databaseScripts);
            this.unitTesting = this.cleanUnitTestTextAdvanced(this.unitTesting);
            this.FunctionalTesting = this.cleanUnitTestTextAdvanced(this.FunctionalTesting);
            this.IntegrationTesting = this.cleanUnitTestTextAdvanced(this.IntegrationTesting);
            this.createFunctiontesttree(this.FunctionalTesting);
            this.createUnittesttree(this.unitTesting);
            this.createIntegrationtesttree(this.IntegrationTesting);
            this.createdatabasetree(this.databaseScripts);


            this.projectStructureTemplate = this.extractSolutionStructure(this.outputText2);
            if (this.selectedTabs['Documentation'] && this.isGreenfield) {
              this.templates = {
                hld: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.hldtemplate,
                lld: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.lldtemplate,
                userManual: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.usermanualtemplate,
                traceabilityMatrix: this.TraceabilityMatrixtemplate
              };

            }
            console.log("response: ", response);
            this.synfetchFolderStructure();

            this.isAnalyzingBLU = false;
            this.apicallvariable = false;
            this.responseBLU = true;
            // this.projectLifecycleData.blueprintingStatus = 'Completed';
            // await this.syncToBackend('Blueprinting Completed');
            this.BLUfeedback = 'RESPONSE RECEIVED'

            // log
            this.apiService.logFrontend(`[INFO] ${'blueprinting successful'}`).subscribe();

          },
          (error) => {
            console.error('Error:', error);

            // log
            this.apiService.logFrontend(`[ERROR] ${JSON.stringify(error)}`).subscribe();
            this.isAnalyzingBLU = false;
            this.apicallvariable = false;
            this.BLUfeedback = 'RESPONSE FAILED'

            //Handle the error here
          }
        );
    }
    else {
      this.BLUfeedback = 'IRSS NOT FOUND'
    }

  }

  //#endregion







  /* --------------------------------------------------
     Reverse Engineering Section 
  -------------------------------------------------- */



  /* --------------------------------------------------
     Reverse Engineering Section
  -------------------------------------------------- */
  //#region Reverse Engineering
  /** Decide at runtime if a file should be read as UTF-8 text */
  extractFieldValue(input: string): string | null {
    // Regex explanation:
    // greenfield/brownfield:
    //   - followed by optional spaces
    //   - optionally ends a line
    //   - possibly followed by newline(s) and more spaces
    //   - captures brown/blue/green in any case
    const regex = /greenfield\/brownfield:\s*(?:\r?\n\s*)?(\b(brown|green)\b)/i;

    const match = input.match(regex);
    return match ? match[1].toLowerCase() : null; // always returns lower-case
  }


  private isTextFile(fileName: string): boolean {
    const lower = fileName.toLowerCase();
    const ext = lower.includes('.') ? lower.split('.').pop()! : '';
    // treat filenames without a dot but matching known names as text
    if (!ext && this.TEXT_EXTS.has(lower)) { return true; }
    return this.TEXT_EXTS.has(ext);
  }

  private async parseZip(file: File): Promise<FileSystemNode[]> {
    const zip = await JSZip.loadAsync(await file.arrayBuffer());
    const tree: FileSystemNode[] = [];

    for (const path in zip.files) {
      const entry = zip.files[path];
      await this.addZipEntry(tree, entry);
    }
    return tree;
  }

  private async addZipEntry(tree: FileSystemNode[], entry: JSZipObject): Promise<void> {
    const parts = entry.name.split('/').filter(Boolean);
    if (!parts.length) { return; }

    const parent = this.ensureFolderChain(tree, parts.slice(0, -1));
    const leaf = parts[parts.length - 1];

    if (entry.dir) {
      const fullPath = entry.name.replace(/\/$/, '');
      this.ensureFolder(parent.children!, leaf, fullPath);
    } else {
      let code: string;
      if (this.isTextFile(leaf)) {
        code = await entry.async('string');
      } else {
        code = `⚠️  "${leaf}" is binary or unsupported; content omitted.`;
      }
      parent.children!.push({
        name: leaf,
        type: 'file',
        content: '',
        code,
        expanded: false,
      });
    }
  }

  private ensureFolderChain(tree: FileSystemNode[], folders: string[]): FileSystemNode {
    let curr: FileSystemNode = { name: '', type: 'folder', content: '', code: '', expanded: true, children: tree };
    const pathParts: string[] = [];

    for (const f of folders) {
      pathParts.push(f);
      let next = curr.children!.find(c => c.name === f && c.type === 'folder');
      if (!next) {
        next = { name: f, type: 'folder', content: pathParts.join('/'), code: '', expanded: true, children: [] };
        curr.children!.push(next);
      }
      curr = next;
    }
    return curr;
  }

  private ensureFolder(tree: FileSystemNode[], name: string, path: string): void {
    if (!tree.find(n => n.name === name && n.type === 'folder')) {
      tree.push({ name, type: 'folder', content: path, code: '', expanded: true, children: [] });
    }
  }

  /* ───────── helper to create a folder node quickly ───────── */
  private makeFolder(name: string, children: FileSystemNode[] = [], content: string): FileSystemNode {
    return {
      name,
      type: 'folder',
      content: content,
      code: '',
      expanded: true,
      children
    };
  }
solutionName = ""
  /* ───────── upload SOLUTION structure ───────── */
  async onSolutionZipSelected(ev: Event) {
    this.settodefault(1);
    this.inputText = '';
    this.outputText2 = '';
    const inp = ev.target as HTMLInputElement;
    if (!inp.files?.length) return;

    this.loading = true;
    try {
      const zipFile = inp.files[0];

      /* 1. parse the entire ZIP contents */
      const zipContents = await this.parseZip(zipFile);
      this.isLoading = true;

      /* 2. find the first top-level folder (this is our solution) */
      const solutionFolder = zipContents.find(n => n.type === 'folder');
      if (!solutionFolder) {
        console.error('No solution folder found in ZIP');
        return;
      }
      const solutionName = solutionFolder.name;
      const projects = solutionFolder.children || [];
      this.solutionName = solutionName;


      /* 3. create: solution folder > root folder > projects */
      const rootNode = this.makeFolder(solutionName, projects, "root folder");
      const solutionNode = this.makeFolder(solutionName, [rootNode], "solution name");

      /* 4. expose & document */
      this.fileTree = [solutionNode];
      this.projectStructureDescription = this.buildDocument(solutionName, projects);

    } finally {
      this.loading = false;
      this.isLoading = false;

      inp.value = '';
    }
    this.folderStructure = this.fileTree
    this.folderStructureboolean = true;
    this.isLoading = false;

    this.isBrownfield = true;
    this.isGreenfield = false;
    await this.initializeAndSaveProject();
    this.projectLifecycleData.projectType = "Brownfield";
    this.projectLifecycleData.projectName = this.solutionName;
    await this.syncToBackend('Project Type Completed');
    // this.enableReverseEngineering = true;
  }

  /* ───────── upload PROJECT structure ───────── */
  async onProjectZipSelected(ev: Event) {
    this.settodefault(1);
    this.inputText = '';
    this.outputText2 = '';

    const inp = ev.target as HTMLInputElement;
    if (!inp.files?.length) return;

    this.loading = true;
    try {
      const zipFile = inp.files[0];

      /* 1. parse the entire ZIP contents */
      this.isLoading = true;
      const zipContents = await this.parseZip(zipFile);

      /* 2. find the first top-level folder (this is our project) */
      const projectFolder = zipContents.find(n => n.type === 'folder');
      if (!projectFolder) {
        console.error('No project folder found in ZIP');
        return;
      }

      const projectName = projectFolder.name;
      this.solutionName = projectName;

      /* 3. create: solution folder > root folder > project folder > contents */
      const projectNode = this.makeFolder(projectName, projectFolder.children || [], projectName);
      const rootNode = this.makeFolder(projectName, [projectNode], "root folder");
      const solutionNode = this.makeFolder(projectName, [rootNode], "solution name");

      /* 4. expose & document */
      this.fileTree = [solutionNode];
      this.projectStructureDescription = this.buildDocument(projectName, [projectNode]);


    } finally {
      this.loading = false;
      this.isLoading = false;

      
      inp.value = '';
    }
    this.folderStructure = this.fileTree;
    this.folderStructureboolean = true;
    this.isLoading = false;


    this.isBrownfield = true;
    this.isGreenfield = false;
    await this.initializeAndSaveProject();
    this.projectLifecycleData.projectType = "Brownfield";
    this.projectLifecycleData.projectName = this.solutionName;
    await this.syncToBackend('Project Type Completed');
  }



  /* ─────────── Build the document (unchanged) ─────────── */
  private buildDocument(rootName: string, topLevelNodes: FileSystemNode[]): string {
    const out: string[] = [];
    out.push(`Solution Name: ${rootName}`);
    out.push(`Root folder:    ${rootName}`);
    out.push('');

    // every direct child folder of the root = a project
    const projects = topLevelNodes.filter(n => n.type === 'folder') as FileSystemNode[];

    interface FileInfo { node: FileSystemNode; dirPath: string; }
    const collectFiles = (folder: FileSystemNode, currPath: string, buf: FileInfo[]) => {
      for (const c of folder.children ?? []) {
        if (c.type === 'file') buf.push({ node: c, dirPath: currPath });
        else collectFiles(c, `${currPath}/${c.name}`, buf);
      }
    };

    for (const proj of projects) {
      const projPath = `${rootName}/${proj.name}`;
      out.push(`Project Name: ${proj.name}`);
      // out.push(`project path: ${projPath}`);
      out.push('');

      const files: FileInfo[] = [];
      collectFiles(proj, projPath, files);
      for (const f of files) {
        const path1 = `${f.dirPath}`.split('/');
        const path2 = path1.slice(1).join('/');
        out.push(`Project Path: ${path2}`);
        out.push(`File Name: ${f.node.name}`);
        // out.push(f.node.code);
        out.push('');
      }
    }

    return out.join('\n');
  }
  toggleDropdownoptions() { this.dropdownOpen = !this.dropdownOpen; }

  abortREV() {
    if (this.apiREV) {
      this.apiREV.unsubscribe();
      this.isAnalyzingBLU = false;
      this.apicallvariable = false;
      this.abortedREV = true;
      this.BLUfeedback = 'You stopped this response'
    }
  }
  reverseEngineeringAlreadyCalled = false;
  // payload = {};
  callreverseEngineering() {
    this.BLUfeedback = '';
      if (this.folderStructureboolean) {
        if (this.selectedTabs['Project Structure'] ){
        this.reverseEngineeringAlreadyCalled = false
        }
        else{
          this.reverseEngineeringAlreadyCalled = true
        }
      const payload = {
        SolutionDescription: this.projectStructureDescription,
        FolderStructure: this.folderStructure[0],
        UploadChecklist: this.UploadChecklist,
        UploadBestPractice: this.UplaodBestPractice,
        EnterLanguageType: this.EnterLanguageType,
        generateBRDWithTRD: this.generateBRDWithTRD,
        solutionOverview: this.solutionOverview,
        requirementSummary: this.requirementSummary,
        reverseEngineeringAlreadyCalled: this.reverseEngineeringAlreadyCalled
      };

      this.isAnalyzingBLU = true;
      this.responseBLU = false;
      this.abortedREV = false;
      this.apicallvariable = true;
      this.apiREV = this.apiService.ReverseEngineering(payload).subscribe(
        resp => {
          const backendRawResponse = resp;
          // resp.SolutionDescription and resp.FolderStructure per backend dictionary keys
          
          if (!this.reverseEngineeringAlreadyCalled) {
              const backendSolutionDescription = resp['SolutionDescription'];
              const backendProjectStructure = resp['FolderStructure'];
              this.folderStructure = [backendProjectStructure];
              this.projectStructureDescription = backendSolutionDescription;
              this.solutionOverview = resp['SolutionOverview'];
              this.solutionOverview2 = resp['SolutionOverview'];
              this.requirementSummary = resp['requirementSummary'];
            }

          if (this.generateBRDWithTRD) {
            this.inputText = resp['BRDwithTRD']
            // this.reverseEngineeringAlreadyCalled = false
          }
          else if (!this.generateBRDWithTRD) {
            this.inputText = "";
          }

          this.projectStructureTemplate = this.extractSolutionStructure(this.solutionOverview);
          this.solutionOverview2 = this.extractSolutionOverviewSection(this.solutionOverview);

          this.templates = {
            hld: "Solution Overview: \n" + this.solutionOverview2 + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "requirementsummary:\n" + this.requirementSummary + "\n\n" + "Document Template: \n" + this.hldtemplate,
            lld: "Solution Overview: \n" + this.solutionOverview2 + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "requirementsummary:\n" + this.requirementSummary + "\n\n" + "Document Template: \n" + this.lldtemplate,
            userManual: "Solution Overview: \n" + this.solutionOverview2 + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "requirementsummary:\n" + this.requirementSummary + "\n\n" + "Document Template: \n" + this.usermanualtemplate,
            traceabilityMatrix: this.TraceabilityMatrixtemplate
          };
          // this.solutionOverview = this.solutionOverview2;

          this.isAnalyzingBLU = false;
          this.responseBLU = true;
          this.BLUfeedback = 'RESPONSE RECEIVED'
          this.apicallvariable = false;

        },
        err => {
          console.error("API call error:", err);
          this.BLUfeedback = 'ERROR OCCURRED DURING API CALL';
          this.isAnalyzingBLU = false;
          this.apicallvariable = false;
        }
      );
    }
    else {
      this.BLUfeedback = 'NO STRUCTURE FOUND'
    }
  }

  //#endregion









  /* --------------------------------------------------
     Folder Structure Logic
  -------------------------------------------------- */
  //#region Folder Structure Logic

  //Project Solution Tree
  fetchFolderStructure(structure: string) {
    const inputString = structure;

    this.folderStructure = this.parseStructure(inputString);
    // log
    this.apiService.logFrontend(`[INFO] ${'folder structure parsed'}`).subscribe();
  }



  parseStructure(input: string): Node[] {
    /* header regex patterns ------------------------------------------------ */
    const RX = {
      solution: /^\s*[-#\s]*solution\s+name\s*:\s*(.+)$/i,
      root: /^\s*[-#\s]*root\s+folder\s*:\s*(.+)$/i,
      projectName: /^\s*[-#\s]*project\s+name\s*:\s*(.+)$/i,    // IGNORE completely
      path: /^\s*[-#\s]*project\s+path\s*:\s*(.+)$/i,
      // Enhanced file regex: captures filename (with extension) and optional content (with or without brackets)
      file: /^\s*[-#\s]*file\s+name\s*:\s*([^\s\(]+(?:\.[^\s\(]+)?)\s*(?:\(([^)]*)\)|(.*?))?\s*$/i,
      // Database object pattern - case insensitive with optional prefix/suffix ":"
      databaseObject: /^[:\s]*database\s+object[:\s]*$/i
    };

    /* recursive folder builder with path tracking --------------------------- */
    const ensureFolder = (parent: Node, segs: string[], basePath: string = '', idx = 0): Node => {
      if (idx === segs.length) return parent;
      parent.children ??= [];

      const seg = segs[idx];
      const currentPath = basePath ? `${basePath}/${seg}` : seg;

      let node = parent.children.find(f => f.type === 'folder' && f.name === seg);
      if (!node) {
        node = {
          name: seg,
          type: 'folder',
          expanded: true,
          children: [],
          content: currentPath
        };
        parent.children.push(node);
      }
      return ensureFolder(node, segs, currentPath, idx + 1);
    };

    /* state variables ------------------------------------------------------- */
    const solution: Node = { name: '', type: 'folder', expanded: true, children: [], content: '' };

    let rootName = '';
    let rootNode: Node | null = null;
    let currentFolder: Node = solution;
    let currentFile: Node | null = null;
    let buffer: string[] = [];
    let skipContent = false; // Flag to skip content after database object

    const flush = () => {
      if (currentFile && buffer.length) {
        const bufferContent = buffer.join('\n').trim();
        if (currentFile.content && bufferContent) {
          currentFile.content += '\n' + bufferContent;
        } else if (bufferContent) {
          currentFile.content = bufferContent;
        }
        buffer = [];
      }
    };

    /* line processing ------------------------------------------------------- */
    const lines = input.includes('\n')
      ? input.split(/\r?\n/)
      : input.split(/\s+/).reduce<string[]>((arr, t) => {
        if (!t) return arr;
        arr[arr.length - 1] = (arr[arr.length - 1] || '') + (arr[arr.length - 1] ? ' ' : '') + t;
        if (t.endsWith(':')) arr.push('');
        return arr;
      }, ['']);

    /* main parsing loop ----------------------------------------------------- */
    for (const raw of lines) {
      const line = raw.trim();
      if (!line) continue;

      let m: RegExpMatchArray | null;

      /* check for database object pattern ---------------------------------- */
      if (line.match(RX.databaseObject)) {
        skipContent = true;
        continue;
      }

      /* if we're skipping content after database object, ignore everything - */
      if (skipContent) {
        continue;
      }

      /* solution name ------------------------------------------------------- */
      if ((m = line.match(RX.solution))) {
        flush();
        solution.name = m[1].trim();
        solution.content = 'solution name';
        currentFolder = solution;
        currentFile = null;
        continue;
      }

      /* root folder --------------------------------------------------------- */
      if ((m = line.match(RX.root))) {
        flush();
        rootName = m[1].trim();
        rootNode = ensureFolder(solution, [rootName]);
        rootNode.content = 'root folder';
        currentFolder = rootNode;
        currentFile = null;
        continue;
      }

      /* project name → COMPLETELY IGNORE ----------------------------------- */
      if (line.match(RX.projectName)) {
        continue;
      }

      /* project path -------------------------------------------------------- */
      if ((m = line.match(RX.path))) {
        flush();
        let segs = m[1].replace(/\\/g, '/').split('/').filter(Boolean);
        const originalPath = m[1].replace(/\\/g, '/');

        // Strip solution/root names if they appear at start of path
        let pathBase = '';
        if (solution.name && segs[0] === solution.name) {
          segs.shift();
          pathBase = solution.name;
        }
        if (rootName && segs[0] === rootName) {
          segs.shift();
          pathBase = pathBase ? `${pathBase}/${rootName}` : rootName;
        }

        let base: Node = solution;
        if (rootNode && rootName) base = rootNode;

        currentFolder = ensureFolder(base, segs, pathBase);
        currentFile = null;
        continue;
      }

      /* file name (with optional inline content - bracketed or unbracketed) */
      if ((m = line.match(RX.file))) {
        flush();
        const fileName = m[1].trim();
        let inlineContent = '';

        // Check if content is in brackets (group 2) or unbracketed (group 3)
        if (m[2]) {
          inlineContent = m[2].trim();  // Content in brackets
        } else if (m[3]) {
          inlineContent = m[3].trim();  // Content without brackets
        }

        const fileNode: Node = {
          name: fileName,
          type: 'file',
          expanded: true,
          content: inlineContent
        };

        currentFolder.children ??= [];
        currentFolder.children.push(fileNode);
        currentFile = fileNode;
        continue;
      }

      /* everything else → file content ------------------------------------- */
      if (currentFile) {
        buffer.push(raw);
      }
    }

    flush();
    return [solution];
  }

  //Clean text for tree
  cleanUnitTestTextAdvanced(rawText: string): string {
    return rawText
      .split('\n')
      .map(line =>
        line
          .replace(/^[#*•\s]+/, '') // Remove leading bullets or symbols
          .replace(/[*_]{1,2}(.*?)[*_]{1,2}/g, '$1') // Remove markdown bold/italic
          .trim()
      )
      .filter(line => line.length > 0)
      .join('\n');
  }
  //Database Folder Structure
  createdatabasetree(databasescript: string) {
    const input = databasescript;

    this.datascripttree = this.parseDatabaseScript(input);
  }
  parseDatabaseScript(input: string): any[] {
    console.log("inside the data structure");

    const projects: any[] = [];
    if (!input) return projects;

    // Normalize line endings and split into lines
    const lines = input.replace(/\r\n/g, '\n').split('\n').map(line => line.trim()).filter(line => line);

    const saveCurrentFile = (file: any, content: string): void => {
      if (file && content.trim()) file.content = content.trim();
    };

    const saveFolderContent = (folder: any, content: string): void => {
      if (folder && content.trim()) folder.content = content.trim();
    };

    let currentMainFolder: any = null;
    let currentDatabaseFolder: any = null;
    let currentSubFolder: any = null;
    let currentFile: any = null;
    let fileContent: string = '';
    let folderContent: string = '';

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Check for Database Script (main folder, case-insensitive, flexible prefix)
      if (/^[-_\.\d\s]*database\s+script\b/i.test(line)) {
        saveCurrentFile(currentFile, fileContent);
        currentFile = null;
        fileContent = '';
        currentMainFolder = {
          name: 'Database Script',
          type: 'folder',
          expanded: false,
          children: [],
          content: '/Database Script'
        };
        projects.push(currentMainFolder);
        currentDatabaseFolder = null;
        currentSubFolder = null;
        folderContent = '';
        continue;
      }

      // Check for Database Name (case-insensitive, flexible prefix)
      const databaseNameMatch = line.match(/^[-_\.\d\s]*database\s+name:\s*(.*)/i);
      if (databaseNameMatch && currentMainFolder) {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentMainFolder, folderContent);
        currentDatabaseFolder = {
          name: databaseNameMatch[1].trim(),
          type: 'folder',
          expanded: false,
          children: [],
          content: `/${databaseNameMatch[1].trim()}`
        };
        currentMainFolder.children.push(currentDatabaseFolder);
        currentSubFolder = null;
        currentFile = null;
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Tables section (case-insensitive, flexible prefix)
      if (/^[-_\.\d\s]*tables\b/i.test(line)) {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentDatabaseFolder, folderContent);
        if (currentDatabaseFolder) {
          currentSubFolder = {
            name: 'Tables',
            type: 'folder',
            expanded: false,
            children: [],
            content: '/Tables'
          };
          currentDatabaseFolder.children.push(currentSubFolder);
        }
        currentFile = null;
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Views section (case-insensitive, flexible prefix)
      if (/^[-_\.\d\s]*views\b/i.test(line)) {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        if (currentDatabaseFolder) {
          currentSubFolder = {
            name: 'Views',
            type: 'folder',
            expanded: false,
            children: [],
            content: '/Views'
          };
          currentDatabaseFolder.children.push(currentSubFolder);
        }
        currentFile = null;
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Stored Procedures section (case-insensitive, flexible prefix)
      if (/^[-_\.\d\s]*stored\s+procedures\b/i.test(line)) {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        if (currentDatabaseFolder) {
          currentSubFolder = {
            name: 'Stored Procedures',
            type: 'folder',
            expanded: false,
            children: [],
            content: '/Stored Procedures'
          };
          currentDatabaseFolder.children.push(currentSubFolder);
        }
        currentFile = null;
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Functions section (case-insensitive, flexible prefix)
      if (/^[-_\.\d\s]*functions\b/i.test(line)) {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        if (currentDatabaseFolder) {
          currentSubFolder = {
            name: 'Functions',
            type: 'folder',
            expanded: false,
            children: [],
            content: '/Functions'
          };
          currentDatabaseFolder.children.push(currentSubFolder);
        }
        currentFile = null;
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Table Name (case-insensitive, flexible prefix)
      const tableNameMatch = line.match(/^[-_\.\d\s]*table\s+name:\s*(.*)/i);
      if (tableNameMatch && currentSubFolder && currentSubFolder.name === 'Tables') {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        currentFile = {
          name: tableNameMatch[1].trim(),
          type: 'file',
          content: ''
        };
        currentSubFolder.children.push(currentFile);
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for View Name (case-insensitive, flexible prefix)
      const viewNameMatch = line.match(/^[-_\.\d\s]*view\s+name:\s*(.*)/i);
      if (viewNameMatch && currentSubFolder && currentSubFolder.name === 'Views') {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        currentFile = {
          name: viewNameMatch[1].trim(),
          type: 'file',
          content: ''
        };
        currentSubFolder.children.push(currentFile);
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for SP Name (case-insensitive, flexible prefix)
      const spNameMatch = line.match(/^[-_\.\d\s]*sp\s+name:\s*(.*)/i);
      if (spNameMatch && currentSubFolder && currentSubFolder.name === 'Stored Procedures') {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        currentFile = {
          name: spNameMatch[1].trim(),
          type: 'file',
          content: ''
        };
        currentSubFolder.children.push(currentFile);
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Check for Function Name (case-insensitive, flexible prefix)
      const functionNameMatch = line.match(/^[-_\.\d\s]*function\s+name:\s*(.*)/i);
      if (functionNameMatch && currentSubFolder && currentSubFolder.name === 'Functions') {
        saveCurrentFile(currentFile, fileContent);
        saveFolderContent(currentSubFolder, folderContent);
        currentFile = {
          name: functionNameMatch[1].trim(),
          type: 'file',
          content: ''
        };
        currentSubFolder.children.push(currentFile);
        fileContent = '';
        folderContent = '';
        continue;
      }

      // Add content to appropriate container (not a header)
      const isHeader = /^[-_\.\d\s]*(?:database\s+script|database\s+name|tables|views|stored\s+procedures|functions|table\s+name|view\s+name|sp\s+name|function\s+name):?/i.test(line);
      if (!isHeader) {
        if (currentFile) {
          fileContent += (fileContent ? '\n' : '') + line;
        } else {
          folderContent += (folderContent ? '\n' : '') + line;
        }
      }
    }

    // Save any remaining content
    saveCurrentFile(currentFile, fileContent);
    saveFolderContent(currentSubFolder || currentDatabaseFolder || currentMainFolder, folderContent);

    return projects;
  }

  //Unittesting Folder Structure
  createUnittesttree(unittesting: string) {
    const temp = unittesting;

    this.unittestingtree = this.parseText(temp);

  }
  // parseText(input: string): any[] {
  //   console.log("inside the unit structure");

  //   const projects: any[] = [];

  //   // Split input into project sections (allows for numbers, punctuation, etc. before "project")
  //   const projectSections = input.split(/(?=\bproject\b|\bproject\s+name\b)/i)
  //     .filter(section => section.trim())
  //     .filter(section => section.match(/(project\s*name:|project:)/i));

  //   for (const section of projectSections) {
  //     const project: any = {
  //       type: 'folder',
  //       expanded: false,
  //       children: [],
  //       content: ''
  //     };

  //     // Extract project name (case-insensitive, allows numbers, punctuation, etc. before)
  //     const projectNameRegex = /.*?(?:project\s*name:|project:)\s*(.*?)(?=\n|$)/i;
  //     const projectNameMatch = section.match(projectNameRegex);
  //     if (projectNameMatch) {
  //       project.name = projectNameMatch[1].trim();
  //       project.content = `/${project.name}`;
  //     }

  //     // Split section into file blocks (allows for numbers, punctuation, etc. before "file")
  //     const fileBlocks = section.split(/(?=\bfile\b|\bfile\s+name\b)/i)
  //       .filter(block => block.trim())
  //       .filter(block => block.match(/(file\s*name:|file:)/i));

  //     for (const fileBlock of fileBlocks) {
  //       const file: any = { type: 'file', content: '', name: '' };

  //       // Extract file name (case-insensitive, allows numbers, punctuation, etc. before)
  //       const fileNameRegex = /.*?(?:file\s*name:|file:)\s*(.*?)(?=\n|$)/i;
  //       const fileNameMatch = fileBlock.match(fileNameRegex);
  //       if (fileNameMatch) {
  //         file.name = fileNameMatch[1].trim();
  //       }

  //       // Extract everything after the file name line as content
  //       const contentMatch = fileBlock.match(/(?:file\s*name:|file:).*?\n([\s\S]*)/i);
  //       if (contentMatch) {
  //         let content = contentMatch[1].trim();
  //         content = content
  //           .split('\n')
  //           .map(line => line.trim())
  //           .filter(line => line)
  //           .join('\n');
  //         file.content = content;
  //       }

  //       if (file.name) {
  //         project.children.push(file);
  //       }
  //     }

  //     if (project.name && project.children.length > 0) {
  //       projects.push(project);
  //     }
  //   }

  //   return projects;
  // }


  parseText(input: string): any[] {
    const RX = {
      project: /^\s*[-#\s]*(?:\d+\.?\s*)?project\s*(?:name\s*)?:\s*(.+)$/i,
      file: /^\s*[-#\s]*(?:\d+\.?\s*)?file\s*(?:name\s*)?:\s*([^\s\(]+(?:\.[^\s\(]+)?)\s*(?:\(([^)]*)\)|(.*?))?\s*$/i,
      fileMarker: /^\s*[-#\s]*(?:\d+\.?\s*)?file\s*(?:name\s*)?:\s*$/i, // File marker without name
      path: /^\s*[-#\s]*(?:project\s+)?path\s*:\s*(.+)$/i
    };

    const projects: any[] = [];
    let currentProject: any = null;
    let currentFile: any = null;
    let buffer: string[] = [];
    let waitingForFileName = false;

    const flush = () => {
      if (currentFile && buffer.length) {
        const bufferContent = buffer.join('\n').trim();
        if (currentFile.content && bufferContent) {
          currentFile.content += '\n' + bufferContent;
        } else if (bufferContent) {
          currentFile.content = bufferContent;
        }
        buffer = [];
      }
    };

    const ensureFolder = (parent: any, segs: string[]): any => {
      let current = parent;
      for (const seg of segs) {
        current.children = current.children || [];
        let folder = current.children.find((c: any) => c.type === 'folder' && c.name === seg);
        if (!folder) {
          folder = {
            name: seg,
            type: 'folder',
            expanded: false,
            children: [],
            content: `/${seg}`
          };
          current.children.push(folder);
        }
        current = folder;
      }
      return current;
    };

    const lines = input.split(/\r?\n/);

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      if (!trimmed) continue;

      let match: RegExpMatchArray | null;

      // Project detection
      if ((match = trimmed.match(RX.project))) {
        flush();
        currentProject = {
          name: match[1].trim(),
          type: 'folder',
          expanded: false,
          children: [],
          content: `/${match[1].trim()}`
        };
        projects.push(currentProject);
        currentFile = null;
        waitingForFileName = false;
        continue;
      }

      // Path handling for nested structure
      if ((match = trimmed.match(RX.path)) && currentProject) {
        flush();
        const pathSegs = match[1].replace(/\\/g, '/').split('/').filter(Boolean);
        const targetFolder = ensureFolder(currentProject, pathSegs);
        currentFile = null;
        waitingForFileName = false;
        continue;
      }

      // File detection with inline name
      if ((match = trimmed.match(RX.file)) && currentProject) {
        flush();
        const fileName = match[1].trim();
        let inlineContent = '';

        if (match[2]) {
          inlineContent = match[2].trim();
        } else if (match[3]) {
          inlineContent = match[3].trim();
        }

        currentFile = {
          name: fileName,
          type: 'file',
          content: inlineContent
        };

        currentProject.children = currentProject.children || [];
        currentProject.children.push(currentFile);
        waitingForFileName = false;
        continue;
      }

      // File marker without name (name comes on next line)
      if (trimmed.match(RX.fileMarker) && currentProject) {
        flush();
        waitingForFileName = true;
        currentFile = null;
        continue;
      }

      // Handle file name on next line
      if (waitingForFileName && currentProject) {
        // Extract filename and optional inline content
        const fileNameMatch = trimmed.match(/^([^\s\(]+(?:\.[^\s\(]+)?)\s*(?:\(([^)]*)\)|(.*?))?$/);
        if (fileNameMatch) {
          const fileName = fileNameMatch[1].trim();
          let inlineContent = '';

          if (fileNameMatch[2]) {
            inlineContent = fileNameMatch[2].trim();
          } else if (fileNameMatch[3]) {
            inlineContent = fileNameMatch[3].trim();
          }

          currentFile = {
            name: fileName,
            type: 'file',
            content: inlineContent
          };

          currentProject.children = currentProject.children || [];
          currentProject.children.push(currentFile);
          waitingForFileName = false;
          continue;
        }
      }

      // Content accumulation
      if (currentFile && !waitingForFileName) {
        buffer.push(line);
      }
    }

    flush();
    return projects;
  }




  //Functiontesting Folder Structure
  createFunctiontesttree(functiontesting: string) {
    const temp = functiontesting;
    this.functiontestingtree = this.parseText2(temp);
  }

  private handleProjectName2(projectName: string, result: any[], projectMap: Map<string, any>, useCaseMap: Map<string, any>, currentFile: any, fileContent: string, currentUseCaseFolder: any, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentUseCaseFolder, folderContent);

    // Create project even if name is empty
    if (!projectMap.has(projectName)) {
      const projectFolder = {
        name: projectName, // Can be empty string ""
        type: 'folder',
        expanded: false,
        children: [],
        content: projectName ? `/${projectName}` : `/` // Handle empty name in content
      };
      result.push(projectFolder);
      projectMap.set(projectName, projectFolder);
      useCaseMap.set(projectName, new Map<string, any>());
    }
  }

  private handleUseCase(useCaseName: string, currentProjectFolder: any, useCaseMap: Map<string, any>, currentFile: any, fileContent: string, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentProjectFolder, folderContent);

    const projectName = currentProjectFolder.name;
    const projectUseCases = useCaseMap.get(projectName);

    // Create use case even if name is empty
    if (projectUseCases && !projectUseCases.has(useCaseName)) {
      const useCaseFolder = {
        name: useCaseName, // Can be empty string ""
        type: 'folder',
        expanded: false,
        children: [],
        content: useCaseName ? `/${useCaseName}` : `/` // Handle empty name in content
      };
      currentProjectFolder.children.push(useCaseFolder);
      projectUseCases.set(useCaseName, useCaseFolder);
    }
  }

  private handleFileName2(fileName: string, currentUseCaseFolder: any, currentFile: any, fileContent: string, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentUseCaseFolder, folderContent);

    // For empty file names, we still want to create a unique name
    const uniqueFileName = fileName === "" ? this.getUniqueEmptyFileName(currentUseCaseFolder) : this.getUniqueFileName(currentUseCaseFolder, fileName);

    const file = {
      name: uniqueFileName, // Will be "" for first empty file, then "", "_1", "_2", etc.
      type: 'file',
      content: ''
    };
    currentUseCaseFolder.children.push(file);
  }

  // Updated method to handle empty file names
  private getUniqueEmptyFileName(folder: any): string {
    const existingFiles = folder.children.filter((child: any) => child.type === 'file');
    const existingNames = existingFiles.map((file: any) => file.name);

    // For empty file names, start with "" and add numbers if needed
    if (!existingNames.includes("")) {
      return "";
    }

    let counter = 1;
    let uniqueName = `_${counter}`;

    while (existingNames.includes(uniqueName)) {
      counter++;
      uniqueName = `_${counter}`;
    }

    return uniqueName;
  }


  parseText2(input: string): any[] {
    const result: any[] = [];
    const projectMap = new Map<string, any>(); // Track existing projects
    const useCaseMap = new Map<string, any>(); // Track existing use cases per project

    const lines = input.split('\n').map((line: string) => line.trim()).filter((line: string) => line);

    let currentProjectFolder: any = null;
    let currentUseCaseFolder: any = null;
    let currentFile: any = null;
    let fileContent: string = '';
    let folderContent: string = '';

    // State tracking for multi-line headers
    let pendingHeader: { type: string; index: number } | null = null;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Check if we're expecting a value from previous line
      if (pendingHeader && i === pendingHeader.index + 1) {
        const headerType = pendingHeader.type;
        let value = line.trim();

        // CRITICAL: Check if the current line is actually another header
        const isAnotherHeader = this.isValidHeader(line);

        if (isAnotherHeader) {
          // This line is another header, not the value we're expecting
          // Create node with empty name for the pending header
          if (headerType === 'project') {
            this.handleProjectName2("", result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
            currentProjectFolder = projectMap.get("");
            currentUseCaseFolder = null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else if (headerType === 'usecase' && currentProjectFolder) {
            this.handleUseCase("", currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
            const projectUseCases = useCaseMap.get(currentProjectFolder.name);
            currentUseCaseFolder = projectUseCases?.get("") || null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else if (headerType === 'file' && currentUseCaseFolder) {
            this.handleFileName2("", currentUseCaseFolder, currentFile, fileContent, folderContent);
            currentFile = currentUseCaseFolder.children[currentUseCaseFolder.children.length - 1];
            fileContent = '';
            folderContent = '';
          }

          pendingHeader = null;
          i--; // Reprocess this line as a header
          continue;
        }

        // Process the value (even if it's empty string)
        if (headerType === 'project') {
          this.handleProjectName2(value, result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
          currentProjectFolder = projectMap.get(value);
          currentUseCaseFolder = null;
          currentFile = null;
          fileContent = '';
          folderContent = '';
        } else if (headerType === 'usecase' && currentProjectFolder) {
          this.handleUseCase(value, currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
          const projectUseCases = useCaseMap.get(currentProjectFolder.name);
          currentUseCaseFolder = projectUseCases?.get(value) || null;
          currentFile = null;
          fileContent = '';
          folderContent = '';
        } else if (headerType === 'file' && currentUseCaseFolder) {
          this.handleFileName2(value, currentUseCaseFolder, currentFile, fileContent, folderContent);
          currentFile = currentUseCaseFolder.children[currentUseCaseFolder.children.length - 1];
          fileContent = '';
          folderContent = '';
        }

        pendingHeader = null;
        continue;
      }

      // Check if this line is a valid header (starts with header pattern)
      const isValidHeaderLine = this.isValidHeader(line);

      if (isValidHeaderLine) {
        // Enhanced Project Name matching - handles 0-4 character prefix and next line logic
        const projectNameMatch = line.match(/^(.{0,4})project\s+name\s*:\s*(.*?)$/i);
        if (projectNameMatch) {
          const projectName = projectNameMatch[2].trim();

          if (projectName) {
            // Project name is on the same line and not empty
            this.handleProjectName2(projectName, result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
            currentProjectFolder = projectMap.get(projectName);
            currentUseCaseFolder = null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else {
            // Project name might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'project', index: i };
              } else {
                // Next line is header or empty, create project with empty name
                this.handleProjectName2("", result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
                currentProjectFolder = projectMap.get("");
                currentUseCaseFolder = null;
                currentFile = null;
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create project with empty name
              this.handleProjectName2("", result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
              currentProjectFolder = projectMap.get("");
              currentUseCaseFolder = null;
              currentFile = null;
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }

        // Enhanced Use Case matching - handles 0-4 character prefix and next line logic
        const useCaseMatch = line.match(/^(.{0,4})use\s+case\s*:\s*(.*?)$/i);
        if (useCaseMatch && currentProjectFolder) {
          const useCaseName = useCaseMatch[2].trim();

          if (useCaseName) {
            // Use case is on the same line and not empty
            this.handleUseCase(useCaseName, currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
            const projectUseCases = useCaseMap.get(currentProjectFolder.name);
            currentUseCaseFolder = projectUseCases?.get(useCaseName) || null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else {
            // Use case might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'usecase', index: i };
              } else {
                // Next line is header or empty, create use case with empty name
                this.handleUseCase("", currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
                const projectUseCases = useCaseMap.get(currentProjectFolder.name);
                currentUseCaseFolder = projectUseCases?.get("") || null;
                currentFile = null;
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create use case with empty name
              this.handleUseCase("", currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
              const projectUseCases = useCaseMap.get(currentProjectFolder.name);
              currentUseCaseFolder = projectUseCases?.get("") || null;
              currentFile = null;
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }

        // Enhanced File Name matching - handles 0-4 character prefix and next line logic
        const fileNameMatch = line.match(/^(.{0,4})file\s+name\s*:\s*(.*?)$/i);
        if (fileNameMatch && currentUseCaseFolder) {
          const fileName = fileNameMatch[2].trim();

          if (fileName) {
            // File name is on the same line and not empty
            this.handleFileName2(fileName, currentUseCaseFolder, currentFile, fileContent, folderContent);
            currentFile = currentUseCaseFolder.children[currentUseCaseFolder.children.length - 1];
            fileContent = '';
            folderContent = '';
          } else {
            // File name might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'file', index: i };
              } else {
                // Next line is header or empty, create file with empty name
                this.handleFileName2("", currentUseCaseFolder, currentFile, fileContent, folderContent);
                currentFile = currentUseCaseFolder.children[currentUseCaseFolder.children.length - 1];
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create file with empty name
              this.handleFileName2("", currentUseCaseFolder, currentFile, fileContent, folderContent);
              currentFile = currentUseCaseFolder.children[currentUseCaseFolder.children.length - 1];
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }
      }

      // Skip processing content if we're waiting for a header value on next line
      if (pendingHeader) {
        continue;
      }

      // Add content to appropriate container (only if not a valid header)
      if (!isValidHeaderLine) {
        if (currentFile) {
          fileContent += (fileContent ? '\n' : '') + line;
        } else {
          folderContent += (folderContent ? '\n' : '') + line;
        }
      }
    }

    // Handle any remaining pending header at the end
    if (pendingHeader) {
      const headerType = pendingHeader.type;
      if (headerType === 'project') {
        this.handleProjectName2("", result, projectMap, useCaseMap, currentFile, fileContent, currentUseCaseFolder, folderContent);
      } else if (headerType === 'usecase' && currentProjectFolder) {
        this.handleUseCase("", currentProjectFolder, useCaseMap, currentFile, fileContent, folderContent);
      } else if (headerType === 'file' && currentUseCaseFolder) {
        this.handleFileName2("", currentUseCaseFolder, currentFile, fileContent, folderContent);
      }
    }

    // Save any remaining content
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentUseCaseFolder || currentProjectFolder, folderContent);

    return result;
  }

  // Updated isValidHeader method to handle 0-4 character prefixes
  private isValidHeader(line: string): boolean {
    // Match headers with 0-4 character prefix, followed by header keywords
    return /^.{0,4}(project\s+name|use\s+case|file\s+name)\s*:/i.test(line);
  }








  //IntegrationTest Folder structure
  createIntegrationtesttree(Integrationtesting: string) {
    const temp = Integrationtesting;

    this.Integrationtestingtree = this.parseText3(temp);
  }


  parseText3(input: string): any[] {
    const result: any[] = [];
    const projectMap = new Map<string, any>(); // Track existing projects
    const integrationTitleMap = new Map<string, any>(); // Track existing integration titles per project

    const lines = input.split('\n').map((line: string) => line.trim()).filter((line: string) => line);

    let currentProjectFolder: any = null;
    let currentIntegrationFolder: any = null;
    let currentFile: any = null;
    let fileContent: string = '';
    let folderContent: string = '';

    // State tracking for multi-line headers
    let pendingHeader: { type: string; index: number } | null = null;

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];

      // Check if we're expecting a value from previous line
      if (pendingHeader && i === pendingHeader.index + 1) {
        const headerType = pendingHeader.type;
        let value = line.trim();

        // CRITICAL: Check if the current line is actually another header
        const isAnotherHeader = this.isValidHeader3(line);

        if (isAnotherHeader) {
          // This line is another header, not the value we're expecting
          // Create node with empty name for the pending header
          if (headerType === 'project') {
            this.handleProjectName3("", result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
            currentProjectFolder = projectMap.get("");
            currentIntegrationFolder = null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else if (headerType === 'integration' && currentProjectFolder) {
            this.handleIntegrationTitle3("", currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
            const projectIntegrations = integrationTitleMap.get(currentProjectFolder.name);
            currentIntegrationFolder = projectIntegrations?.get("") || null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else if (headerType === 'file' && currentIntegrationFolder) {
            this.handleFileName3("", currentIntegrationFolder, currentFile, fileContent, folderContent);
            currentFile = currentIntegrationFolder.children[currentIntegrationFolder.children.length - 1];
            fileContent = '';
            folderContent = '';
          }

          pendingHeader = null;
          i--; // Reprocess this line as a header
          continue;
        }

        // Process the value (even if it's empty string)
        if (headerType === 'project') {
          this.handleProjectName3(value, result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
          currentProjectFolder = projectMap.get(value);
          currentIntegrationFolder = null;
          currentFile = null;
          fileContent = '';
          folderContent = '';
        } else if (headerType === 'integration' && currentProjectFolder) {
          this.handleIntegrationTitle3(value, currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
          const projectIntegrations = integrationTitleMap.get(currentProjectFolder.name);
          currentIntegrationFolder = projectIntegrations?.get(value) || null;
          currentFile = null;
          fileContent = '';
          folderContent = '';
        } else if (headerType === 'file' && currentIntegrationFolder) {
          this.handleFileName3(value, currentIntegrationFolder, currentFile, fileContent, folderContent);
          currentFile = currentIntegrationFolder.children[currentIntegrationFolder.children.length - 1];
          fileContent = '';
          folderContent = '';
        }

        pendingHeader = null;
        continue;
      }

      // Check if this line is a valid header (starts with header pattern)
      const isValidHeaderLine = this.isValidHeader3(line);

      if (isValidHeaderLine) {
        // Enhanced Project Name matching - handles 0-4 character prefix and next line logic
        const projectNameMatch = line.match(/^(.{0,3})project\s+name\s*:\s*(.*?)$/i);
        if (projectNameMatch) {
          const projectName = projectNameMatch[2].trim();

          if (projectName) {
            // Project name is on the same line and not empty
            this.handleProjectName3(projectName, result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
            currentProjectFolder = projectMap.get(projectName);
            currentIntegrationFolder = null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else {
            // Project name might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader3(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'project', index: i };
              } else {
                // Next line is header or empty, create project with empty name
                this.handleProjectName3("", result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
                currentProjectFolder = projectMap.get("");
                currentIntegrationFolder = null;
                currentFile = null;
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create project with empty name
              this.handleProjectName3("", result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
              currentProjectFolder = projectMap.get("");
              currentIntegrationFolder = null;
              currentFile = null;
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }

        // Enhanced Integration Title matching - handles 0-4 character prefix and next line logic
        const integrationTitleMatch = line.match(/^(.{0,3})integration\s+title\s*:\s*(.*?)$/i);
        if (integrationTitleMatch && currentProjectFolder) {
          const integrationTitle = integrationTitleMatch[2].trim();

          if (integrationTitle) {
            // Integration title is on the same line and not empty
            this.handleIntegrationTitle3(integrationTitle, currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
            const projectIntegrations = integrationTitleMap.get(currentProjectFolder.name);
            currentIntegrationFolder = projectIntegrations?.get(integrationTitle) || null;
            currentFile = null;
            fileContent = '';
            folderContent = '';
          } else {
            // Integration title might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader3(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'integration', index: i };
              } else {
                // Next line is header or empty, create integration with empty name
                this.handleIntegrationTitle3("", currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
                const projectIntegrations = integrationTitleMap.get(currentProjectFolder.name);
                currentIntegrationFolder = projectIntegrations?.get("") || null;
                currentFile = null;
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create integration with empty name
              this.handleIntegrationTitle3("", currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
              const projectIntegrations = integrationTitleMap.get(currentProjectFolder.name);
              currentIntegrationFolder = projectIntegrations?.get("") || null;
              currentFile = null;
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }

        // Enhanced File Name matching - handles 0-4 character prefix and next line logic
        const fileNameMatch = line.match(/^(.{0,3})file\s+name\s*:\s*(.*?)$/i);
        if (fileNameMatch && currentIntegrationFolder) {
          const fileName = fileNameMatch[2].trim();

          if (fileName) {
            // File name is on the same line and not empty
            this.handleFileName3(fileName, currentIntegrationFolder, currentFile, fileContent, folderContent);
            currentFile = currentIntegrationFolder.children[currentIntegrationFolder.children.length - 1];
            fileContent = '';
            folderContent = '';
          } else {
            // File name might be on the next line
            const nextLineIndex = i + 1;
            if (nextLineIndex < lines.length) {
              const nextLine = lines[nextLineIndex];
              const isNextLineHeader = this.isValidHeader3(nextLine);

              if (!isNextLineHeader && nextLine.trim()) {
                pendingHeader = { type: 'file', index: i };
              } else {
                // Next line is header or empty, create file with empty name
                this.handleFileName3("", currentIntegrationFolder, currentFile, fileContent, folderContent);
                currentFile = currentIntegrationFolder.children[currentIntegrationFolder.children.length - 1];
                fileContent = '';
                folderContent = '';
              }
            } else {
              // No next line, create file with empty name
              this.handleFileName3("", currentIntegrationFolder, currentFile, fileContent, folderContent);
              currentFile = currentIntegrationFolder.children[currentIntegrationFolder.children.length - 1];
              fileContent = '';
              folderContent = '';
            }
          }
          continue;
        }
      }

      // Skip processing content if we're waiting for a header value on next line
      if (pendingHeader) {
        continue;
      }

      // Add content to appropriate container (only if not a valid header)
      if (!isValidHeaderLine) {
        if (currentFile) {
          fileContent += (fileContent ? '\n' : '') + line;
        } else {
          folderContent += (folderContent ? '\n' : '') + line;
        }
      }
    }

    // Handle any remaining pending header at the end
    if (pendingHeader) {
      const headerType = pendingHeader.type;
      if (headerType === 'project') {
        this.handleProjectName3("", result, projectMap, integrationTitleMap, currentFile, fileContent, currentIntegrationFolder, folderContent);
      } else if (headerType === 'integration' && currentProjectFolder) {
        this.handleIntegrationTitle3("", currentProjectFolder, integrationTitleMap, currentFile, fileContent, folderContent);
      } else if (headerType === 'file' && currentIntegrationFolder) {
        this.handleFileName3("", currentIntegrationFolder, currentFile, fileContent, folderContent);
      }
    }

    // Save any remaining content
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentIntegrationFolder || currentProjectFolder, folderContent);

    return result;
  }

  // Updated isValidHeader method for integration title hierarchy
  private isValidHeader3(line: string): boolean {
    // Match headers with 0-4 character prefix, followed by header keywords
    return /^.{0,3}(project\s+name|integration\s+title|file\s+name)\s*:/i.test(line);
  }

  // Helper method to handle project name creation
  private handleProjectName3(projectName: string, result: any[], projectMap: Map<string, any>, integrationTitleMap: Map<string, any>, currentFile: any, fileContent: string, currentIntegrationFolder: any, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentIntegrationFolder, folderContent);

    // Create project even if name is empty
    if (!projectMap.has(projectName)) {
      const projectFolder = {
        name: projectName, // Can be empty string ""
        type: 'folder',
        expanded: false,
        children: [],
        content: projectName ? `${projectName}` : `` // Handle empty name in content
      };
      result.push(projectFolder);
      projectMap.set(projectName, projectFolder);
      integrationTitleMap.set(projectName, new Map<string, any>());
    }
  }

  // Helper method to handle integration title creation
  private handleIntegrationTitle3(integrationTitle: string, currentProjectFolder: any, integrationTitleMap: Map<string, any>, currentFile: any, fileContent: string, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentProjectFolder, folderContent);

    const projectName = currentProjectFolder.name;
    const projectIntegrations = integrationTitleMap.get(projectName);

    // Create integration title even if name is empty
    if (projectIntegrations && !projectIntegrations.has(integrationTitle)) {
      const integrationFolder = {
        name: integrationTitle, // Can be empty string ""
        type: 'folder',
        expanded: false,
        children: [],
        content: integrationTitle ? `${integrationTitle}` : `` // Handle empty name in content
      };
      currentProjectFolder.children.push(integrationFolder);
      projectIntegrations.set(integrationTitle, integrationFolder);
    }
  }

  // Helper method to handle file name creation
  private handleFileName3(fileName: string, currentIntegrationFolder: any, currentFile: any, fileContent: string, folderContent: string): void {
    this.saveCurrentFile(currentFile, fileContent);
    this.saveFolderContent(currentIntegrationFolder, folderContent);

    // For empty file names, we still want to create a unique name
    const uniqueFileName = fileName === "" ? this.getUniqueEmptyFileName3(currentIntegrationFolder) : this.getUniqueFileName3(currentIntegrationFolder, fileName);

    const file = {
      name: uniqueFileName, // Will be "" for first empty file, then "", "_1", "_2", etc.
      type: 'file',
      content: ''
    };
    currentIntegrationFolder.children.push(file);
  }

  // Updated method to handle empty file names for integration hierarchy
  private getUniqueEmptyFileName3(folder: any): string {
    const existingFiles = folder.children.filter((child: any) => child.type === 'file');
    const existingNames = existingFiles.map((file: any) => file.name);

    // For empty file names, start with "" and add numbers if needed
    if (!existingNames.includes("")) {
      return "";
    }

    let counter = 1;
    let uniqueName = `_${counter}`;

    while (existingNames.includes(uniqueName)) {
      counter++;
      uniqueName = `_${counter}`;
    }

    return uniqueName;
  }

  // Helper method to get unique file name for integration hierarchy
  private getUniqueFileName3(folder: any, fileName: string): string {
    const existingFiles = folder.children.filter((child: any) => child.type === 'file');
    const existingNames = existingFiles.map((file: any) => file.name);

    if (!existingNames.includes(fileName)) {
      return fileName;
    }

    let counter = 1;
    let uniqueName = fileName;
    const fileExtension = fileName.includes('.') ? fileName.substring(fileName.lastIndexOf('.')) : '';
    const baseName = fileName.includes('.') ? fileName.substring(0, fileName.lastIndexOf('.')) : fileName;

    while (existingNames.includes(uniqueName)) {
      uniqueName = `${baseName}_${counter}${fileExtension}`;
      counter++;
    }

    return uniqueName;
  }







  //#endregion








  /* --------------------------------------------------
     folder structure helpers
  -------------------------------------------------- */

  //#region Helper Folderstrcture
  fetchFileContent(fileName: string) {
    const findFileContent = (folder: any): string | null => {
      if (folder.type === 'folder' && folder.children) {
        for (const child of folder.children) {
          if (child.type === 'file' && child.name === fileName) {
            return child.content;
          } else if (child.type === 'folder') {
            const content = findFileContent(child);
            if (content) return content;
          }
        }
      }
      return null;
    };

    const content = findFileContent(this.folderStructure[0]);
    if (content) {
      this.selectedContent = content;
    } else {
      this.selectedContent = 'File content not found.';

    }
  }
  showFileContent(item: any) {
    this.selectedContent = item.content || 'No content available.';
  }

  showFolderContent(item: any) {
    this.selectedContent = item.content || 'No content available.';
  }

  showCodeFileContent(item: any) {
    this.selectedCodeContent = item.content || 'No content available.';
  }

  showCodeFolderContent(item: any) {
    this.selectedCodeContent = item.content || 'No content available.';
  }

  toggleFolder(item: any) {
    item.expanded = !item.expanded;
  }

  showCodeContent(content: string) {
    this.selectedCodeContent = content;
  }

  toggleCodeFile(item: any) {
    if ((item.type === 'file') && (item.code || item.description || item.codeReview)) {
      item.expanded = !item.expanded;
    }
  }

  private getUniqueFileName(folder: any, fileName: string): string {
    const existingFiles = folder.children.filter((child: any) => child.type === 'file');
    const existingNames = existingFiles.map((file: any) => file.name);

    if (!existingNames.includes(fileName)) {
      return fileName;
    }

    // If file exists, add a number suffix
    let counter = 1;
    let uniqueName = fileName;
    const fileExtension = fileName.includes('.') ? fileName.substring(fileName.lastIndexOf('.')) : '';
    const baseName = fileName.includes('.') ? fileName.substring(0, fileName.lastIndexOf('.')) : fileName;

    while (existingNames.includes(uniqueName)) {
      uniqueName = `${baseName}_${counter}${fileExtension}`;
      counter++;
    }

    return uniqueName;
  }

  private saveCurrentFile(file: any, content: string): void {
    if (file && content.trim()) {
      file.content = content.trim();
    }
  }

  private saveFolderContent(folder: any, content: string): void {
    if (folder && content.trim()) {
      folder.content = content.trim();
    }
  }

  findFileByName(name: string, folderStructure: any[]): any {
    for (const folder of folderStructure) {
      if (folder.type === 'folder') {
        const result = this.findFileByName(name, folder.children);
        if (result) return result;
      } else if (folder.type === 'file' && folder.name === name) {
        return folder;
      }
    }
    return null;
  }

  formatFileDetails(file: any): string {
    let details = `File Name: ${file.name}\n`;
    for (const [key, value] of Object.entries(file.details)) {
      if (Array.isArray(value)) {
        details += `${key}:\n- ${value.join('\n- ')}\n`;
      } else {
        details += `${key}: ${value}\n`;
      }
    }
    return details;
  }

  toggleCodeFolder(folder: any) {
    folder.expanded = !folder.expanded;
  }

  fetchCodeFileContent(fileName: string) {
    this.selectedCodeFile = fileName;
    this.selectedCodeContent = `// This is the content of ${fileName}\n// Actual code would be displayed here in a real application.`;
  }

  //#endregion







  /* --------------------------------------------------
     Code Synthesis section
  -------------------------------------------------- */

  //#region CodeSynthesisa

  abortCOD() {

    this.istraversing = false;

    this.abortCtrl.abort();                         // cancels in Angular 15+
    this.cancel$.next();                            // always present fallback
    this.cancel$.complete();

    if (this.apiSUM) {
      this.apiSUM.unsubscribe();
    }
    this.responseCOD = false;
    this.isAnalyzingCOD = false;
    this.apicallvariable = false;
    this.abortedCOD = true;
    this.currentSelectedNode = "";
    this.currentProcessingFile = "";


    this.CODfeedback = 'You stopped this response'
  }

  async startCodeSynthesis() {
    this.CODfeedback = ''
    this.generated = 0;
    this.selected = 0;
    this.abortedBLU = false;
    this.abortedCOD = false;

    this.abortedSOL = false;
    this.abortedBRD = false;
    if (this.codeSynthesisFolderStructureboolean) {

      // Traverse and update the folder structure
      await this.checkselected(this.codeSynthesisFolderStructure[0], 0);

      if (this.selected) {
        this.abortCtrl = new AbortController();
        this.cancel$ = new Subject<void>();
        // log
        this.apiService.logFrontend(`[INFO] ${'codesynthesis started'}`).subscribe();
        console.log("codesynthesis started")

        this.istraversing = true;
        this.isAnalyzingCOD = true; // Show "Processing..."
        this.apicallvariable = true;
        this.responseCOD = false; // Hide "RESPONSE RECEIVED"
        // log
        this.apiService.logFrontend(`[INFO] ${'RESPONSE RECEIVED'}`).subscribe();
        console.log("here");
        console.log(this.codeSynthesisFolderStructure[0]);


        this.traverseAndUpdateFolderStructure({ node: this.codeSynthesisFolderStructure[0], level: 0, parentfolder: '', signal: this.abortCtrl.signal }).then(async () => {

          console.log("traverse and update")
          this.isAnalyzingCOD = false; // Hide "Processing..."
          this.apicallvariable = false;
          if (this.istraversing) {
            this.responseCOD = true;
            // this.projectLifecycleData.codeSynthesisStatus = 'Completed';
            // await this.syncToBackend('Code Synthesis Completed');
            this.CODfeedback = 'RESPONSE RECEIVED'
            this.currentSelectedNode = "";
            this.currentProcessingFile = "";
          }
          // else{
          //   this.CODfeedback = `RESPONSE RECEIVED ${this.generated} files generated`

          // }

        });
      }
      else {
        this.CODfeedback = "SELECT ANY FILE OR FOLDER"
      }
      // Show "RESPONSE RECEIVED"
    }
    else {
      this.CODfeedback = 'NO STRUCTURE FOUND'
    }

  }

  synfetchFolderStructure() {

    // this.codeSynthesisFolderStructure = this.parsedStructure(inputStr);
    this.codeSynthesisFolderStructure = JSON.parse(JSON.stringify(this.folderStructure))
    const temp = {
      name: 'DataScripting',
      type: 'folder',
      expanded: false,
      children: this.datascripttree,
    };
    const temp2 = {
      name: 'UnitTest',
      type: 'folder',
      expanded: false,
      children: this.unittestingtree,
    };

    const temp3 = {
      name: 'FunctionalTest',
      type: 'folder',
      expanded: false,
      children: this.functiontestingtree,
    };

    const temp4 = {
      name: 'IntegrationTest',
      type: 'folder',
      expanded: false,
      children: this.Integrationtestingtree,
    };


    //document
    const documentationjson = [{
      name: 'HLD',
      type: 'file',
      expanded: true,
      content: this.templates['hld'],
    },
    {
      name: 'LLD',
      type: 'file',
      expanded: true,
      content: this.templates['lld'],
    },
    {
      name: 'User Manual',
      type: 'file',
      expanded: true,
      content: this.templates['userManual'],   //userManual
    },
    {
      name: 'Traceability matrix',
      type: 'file',
      expanded: true,
      content: this.templates['traceabilityMatrix'],   //userManual
    }]
    const temp5 = {
      name: 'Documentation',
      type: 'folder',
      expanded: false,
      children: documentationjson,
    };

    const temp7 = [{
      name: 'Summary',
      type: 'file',
      content: "",
      expanded: false,
    }];

    const temp6 = {
      name: 'Code Review Summary',
      type: 'folder',
      expanded: false,
      children: temp7,
    };


    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp2);
    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp);
    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp3);
    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp4);
    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp5);
    this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(temp6);
    this.codeSynthesisFolderStructureboolean = true;

  }

  extractCode(temp: any): string {
    const pattern = /```([a-zA-Z0-9_]+)?\s*([\s\S]*?)\s*```/;
    const match = temp.match(pattern);
    if (match && match[2]) {
      return match[2].trim();
    }
    else {
      return temp;
    };
  }

  //   listOftreeNode:  any[] = [];

  //   async findProjectNode(node: any, level: number = 0){

  //     if (level === 2) {
  //         console.log("node at level 2",node.name)
  //         this.listOftreeNode.push({...node});
  //     }
  //     // Recursively traverse children
  //     if (node.children) {
  //         for (const child of node.children) {
  //             await this.findProjectNode(child, level + 1);
  //         }
  //     }
  // }

  //   tracebilitydata = "";
  //   cancelSubject = new Subject<void>();
  //    async sendData2() {
  //     const lengthofnode = this.listOftreeNode.length
  //     console.log("list length",lengthofnode)
  //     for (let i = 0; i < this.listOftreeNode.length; i++) {
  //       const item = this.listOftreeNode[i];
  //       var data = {}
  //       if (i === (this.listOftreeNode.length-1)){
  //         data = {
  //           Info: "last",
  //           FolderStructure: this.listOftreeNode[i]

  //         }
  //       }
  //       else{
  //         data = {
  //           Info: "continue",
  //           FolderStructure: this.listOftreeNode[i]

  //         }
  //       }
  //       try {
  //         const response = await firstValueFrom(this.apiService.sendtreestructure(data).pipe(takeUntil(this.cancelSubject)));
  //         this.tracebilitydata = response;
  //         console.log(`Item ${i + 1} sent:`, response);
  //       } catch (error) {
  //         console.error(`Error sending item ${i + 1}:`, error);
  //       }
  //       // console.log(this.tracebilitydata)
  //     }
  //   }

  //     async sendProjectNodes() {

  //     this.listOftreeNode = []
  //     await this.findProjectNode(this.codeSynthesisFolderStructure[0], 0)

  //     console.log("getlevel",this.listOftreeNode)
  //     await this.sendData2()

  // }


  // Add these properties
  showConfirmDialog = false;
  confirmMessage = '';
  confirmAction: (() => void) | null = null;


  // @HostListener('document:keydown.escape', ['$event'])
  // onEscapeKey(event: KeyboardEvent): void {
  //   if (this.showConfirmDialog) {
  //     this.closeConfirmDialog();
  //   }
  // }


  deleteGenerated(item: any, type: 'code' | 'description' | 'codeReview'): void {
    this.confirmMessage = `Are you sure to delete? \n ${type}`;
    this.confirmAction = () => {
      if (type === 'code') {
        delete item.code;
        delete item.isCodeGenerated;
      } else if (type === 'description') {
        delete item.description;
        delete item.isDescriptionGenerated; // Remove flag
      }
      else if (type === 'codeReview') {
        delete item.codeReview
        delete item.iscodeReviewGenerated; // Remove flag
      }

    };
    this.showConfirmDialog = true;
  }

  // Add these helper methods
  onConfirmYes(): void {
    if (this.confirmAction) {
      this.confirmAction();
    }
    this.closeConfirmDialog();
  }

  onConfirmNo(): void {
    this.closeConfirmDialog();
  }

  closeConfirmDialog(): void {
    this.showConfirmDialog = false;
    this.confirmMessage = '';
    this.confirmAction = null;
  }



  async customerfunction(node: any, level: number = 0, parentfolder: string = '') {
    // console.log("node is here:", node)
    node.expanded = true;
    if (node.type === 'folder' && level === 2) {
      if (node.name === 'DataScripting') {
        parentfolder = 'DataScripting';

      }
      else if (node.name === 'UnitTest') {
        parentfolder = 'UnitTest';

      }
      else if (node.name === 'FunctionalTest') {
        parentfolder = 'FunctionalTest';

      }
      else if (node.name === 'IntegrationTest') {
        parentfolder = 'IntegrationTest';

      }
      else if (node.name === 'Documentation') {
        parentfolder = 'Documentation';

      }
      else if (node.name === 'Code Review Summary') {
        parentfolder = 'Code Review Summary';

      }
      else {
        console.log(`At level 2, but folder name does not match: ${node.name}`);
      }
    }

    // console.log("above try");
    // Process file based on the parentfolder
    if (node.type === 'file') {
      // console.log("hello try")
      try {
        if (parentfolder in MandatoryFolders) {
          node.iscodeReviewGenerated = true;
          node.isDescriptionGenerated = true;
          node.isCodeGenerated = true;
        }
        else {
          // console.log("inside else");
          if (this.isBrownfield) {
            node.isCodeGenerated = false;
            node.isDescriptionGenerated = true;
            node.iscodeReviewGenerated = true;
          } else if(this.isGreenfield) {
            node.iscodeReviewGenerated = true;
            node.isDescriptionGenerated = true;
            node.isCodeGenerated = true;
          }
        }
      }

      catch (error) {
        console.error(`Error during processing file: ${node.name}`, error);
      }
    }



    if (node.children) {
      for (const child of node.children) {
        await this.customerfunction(child, level + 1, parentfolder) // ← propagate the signal);
      }
    }

    // console.log(`↰ finished ${node.name}`);
  }





  generated = 0
  currentSelectedNode: any = null;
  async traverseAndUpdateFolderStructure({ node, level = 0, parentfolder = '', signal, cancel$ = this.cancel$ }: { node: any; level?: number; parentfolder?: string; signal?: AbortSignal; cancel$?: Subject<void>; }): Promise<void> {
    if (!this.istraversing || signal?.aborted) {
      return;
    }

    console.log("top traverseAndUpdateFolderStructure");
    // Set the node as expanded before starting processing
    if (node.type === 'file' && !node.selected) { return; }
    if (node.type === 'folder' && !this.folderHasSelection(node)) { return; }


    // if(node.name === "Traceability matrix"){
    //   return;

    // }
    console.log("above try0");

    if (node.name === 'Summary' && node.type === 'file') {
      console.log("inside summary");

      this.apiSUM = this.apiService.getcodereviewSummary(this.codeSynthesisFolderStructure[0]).subscribe(
        resp => {
          console.log("API raw response:", resp); // ✅ Check if you're even getting anything
          if (!resp) {
            console.error("Empty or undefined response!");
            return;
          }
          const backendsummaryResponse = resp;
          node.content = backendsummaryResponse;
          this.showCodeContent(node.content);
        },
        err => {
          console.error("API call failed", err);
          // handle error as needed
        }
      );
      return;
    }

    // console.log("above try1");
    if (this.iscodeReview) {
      console.log("above try12");
      if (this.isdescribe) {
        console.log("above try13");
        if (node.code != null && node.description != null && node.codeReview != null) { return; }
      }
      else {
        console.log("above try14");
        if (node.code != null && node.codeReview) { return; }
      }
    }
    else {
      console.log("above try15");
      console.log(node.name);

      if (this.isdescribe) {
        console.log("above try16");
        if (node.code != null && node.description != null) { return; }
      }

      if (!this.isdescribe) {

        if (node.code) { return; }
      }
    }

    node.expanded = true;

    console.log("above try2");
    // Check if we're at level 2 and it's a folder named 'DataScripting' or 'UnitTest'
    if (node.type === 'folder' && level === 2) {
      if (node.name === 'DataScripting') {
        parentfolder = 'DataScripting';

      }
      else if (node.name === 'UnitTest') {
        parentfolder = 'UnitTest';

      }
      else if (node.name === 'FunctionalTest') {
        parentfolder = 'FunctionalTest';

      }
      else if (node.name === 'IntegrationTest') {
        parentfolder = 'IntegrationTest';

      }
      else if (node.name === 'Documentation') {
        parentfolder = 'Documentation';

      }
      else {
        console.log(`At level 2, but folder name does not match: ${node.name}`);
      }
    }

    console.log("above try3");
    // Process file based on the parentfolder
    if (node.type === 'file') {
      this.currentSelectedNode = node;
      this.currentProcessingFile = node.name;
      console.log("above try");

      try {
        console.log("inside try4");
        if (parentfolder === 'UnitTest') {
          if (!this.isdescribe && !node.code) {

            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                2,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            node.code = this.extractCode(response);
            node.isCodeGenerated = true; // Add flag
            this.generated = this.generated + 1;
            this.showCodeContent(node.code);
          }
          else if (this.isdescribe) {

            if (node.code === null || !node.code) {
              const response = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  2,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.code = this.extractCode(response);
              node.isCodeGenerated = true; // Add flag
              this.generated = this.generated + 1;
              this.showCodeContent(node.code);

            }

            if (!node.description) {
              const descriptionResponse = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  3,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.description = descriptionResponse;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
            }
            this.showCodeContent(node.description);


          }
          // this.projectLifecycleData.testing_Unit = 'Completed';
          // await this.syncToBackend('Unittesting Completed');
        }
        else if (parentfolder === 'DataScripting') {
          if (!this.isdescribe && !node.code) {
            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                1,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            node.code = this.extractCode(response);
            node.isCodeGenerated = true; // Add flag
            this.generated = this.generated + 1;
            this.showCodeContent(node.code);


          }
          else if (this.isdescribe) {
            if (node.code === null || !node.code) {
              const response = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  1,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.code = this.extractCode(response);
              node.isCodeGenerated = true; // Add flag
              this.generated = this.generated + 1;
              this.showCodeContent(node.code);

            }
            if (!node.description) {
              const descriptionResponse = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  3,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.description = descriptionResponse;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
              this.showCodeContent(node.description);
            }

          }

        } //Functional Test
        else if (parentfolder === 'FunctionalTest') {
          if (!this.isdescribe && !node.code) {

            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                4,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            node.code = this.extractCode(response);
            node.isCodeGenerated = true; // Add flag
            this.generated = this.generated + 1;
            this.showCodeContent(node.code);


          }
          else if (this.isdescribe) {
            // Process code and description
            if (node.code === null || !node.code) {

              const response = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  4,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.code = this.extractCode(response);
              node.isCodeGenerated = true; // Add flag
              this.generated = this.generated + 1;
              this.showCodeContent(node.code);

            }
            if (!node.description) {
              const descriptionResponse = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  3,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.description = descriptionResponse;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
              this.showCodeContent(node.description);

            }

          }

        //  this.projectLifecycleData.testing_Functional = 'Completed';
        // await this.syncToBackend('Functional Testing Completed');
        }
        // IntegrationTest
        else if (parentfolder === 'IntegrationTest') {
          if (!this.isdescribe && !node.code) {

            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                5,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            node.code = this.extractCode(response);
            node.isCodeGenerated = true; // Add flag
            this.generated = this.generated + 1;
            this.showCodeContent(node.code);


          }
          else if (this.isdescribe) {
            // Process code and description
            if (node.code === null || !node.code) {

              const response = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  5,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.code = this.extractCode(response);
              node.isCodeGenerated = true; // Add flag
              this.generated = this.generated + 1;
              this.showCodeContent(node.code);

            }
            if (!node.description) {
              const descriptionResponse = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  3,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.description = descriptionResponse;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
              this.showCodeContent(node.description);
            }

          }


        // this.projectLifecycleData.testing_Integration = 'Completed';
        // await this.syncToBackend('Integration Completed');
        }

        else if (parentfolder === 'Documentation') {

          console.log("inside doc");
          // Process code and description
          if (!node.description) {

            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                6,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            console.log("response :", response);
            node.description = response;
            this.generated = this.generated + 1;
            node.isDescriptionGenerated = true; //
            // if(node.name === "HLD"){
            //   this.projectLifecycleData.doc_HLD = 'Completed';
            // }
            // else if(node.name ==="LLD"){
            //   this.projectLifecycleData.doc_LLD = 'Completed';
            // }
            // else if(node.name ==="User Manual"){
            //   this.projectLifecycleData.doc_UserManual = 'Completed';
            // }
            // await this.syncToBackend('Documentation Completed')
            this.showCodeContent(node.description);

            if (!node.description && node.name === "Traceability matrix" && node.type === 'file') {
              console.log("inside Traceability");
              const field = this.extractFieldValue(this.solutionOverview);
              const response = await firstValueFrom(
                this.apiService.Traceability(this.codeSynthesisFolderStructure[0], this.requirementSummary, field,this.projectLifecycleData.projectID)
                  .pipe(takeUntil(cancel$))
              );

              node.description = response;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
              if(node.name ==="Traceability matrix"){
            //   this.projectLifecycleData.doc_TraceabilityMatrix = 'Completed';
            // await this.syncToBackend('Documentation Completed')
            }
              this.showCodeContent(node.description);
            }

          }
          //     if(!node.description && node.name === "Traceability matrix" && node.type === 'file'){
          //        console.log("inside Traceability");

          //         const response = await firstValueFrom(
          //          this.apiService.Traceability(this.codeSynthesisFolderStructure[0])
          //              .pipe(takeUntil(cancel$))
          //             );

          //       node.description = response;
          //       this.generated = this.generated + 1;
          //  }
        }



        else {

          if (!this.isdescribe && !node.code) {

            const response = await firstValueFrom(
              this.apiService.Codesynthesis(
                node.name,
                node.content,
                0,
                this.dataFlow,
                this.solutionOverview,
                this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                signal                               // ← pass the signal
              ).pipe(takeUntil(cancel$))
            );
            node.code = this.extractCode(response);
            node.isCodeGenerated = true; // Add flag
            this.generated = this.generated + 1;
            // this.projectLifecycleData.code = 'Generated';
            // await this.syncToBackend('Code Synthesis Completed');
            this.showCodeContent(node.code);


          }
          else if (this.isdescribe) {
            // Process code and description
            if (node.code === null || !node.code) {

              const response = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  0,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.code = this.extractCode(response);
              node.isCodeGenerated = true; // Add flag
              this.generated = this.generated + 1;
            //   this.projectLifecycleData.code = 'Generated';
            // await this.syncToBackend('Code Synthesis Completed');
              this.showCodeContent(node.code);

            }
            if (!node.description) {
              const descriptionResponse = await firstValueFrom(
                this.apiService.Codesynthesis(
                  node.name,
                  node.content,
                  3,
                  this.dataFlow,
                  this.solutionOverview,
                  this.commonFunctionalities, this.requirementSummary, "", "", "", "", "",this.projectLifecycleData.projectID,
                  signal                               // ← pass the signal
                ).pipe(takeUntil(cancel$))
              );
              node.description = descriptionResponse;
              node.isDescriptionGenerated = true; //
              this.generated = this.generated + 1;
            //   this.projectLifecycleData.description = 'Generated';
            // await this.syncToBackend('Code Synthesis Completed');
              this.showCodeContent(node.description);
            }

          }



        }
        if ((node.codeReview === null || !node.codeReview) && this.iscodeReview && node.code != null) {

          const response = await firstValueFrom(
            this.apiService.Codesynthesis(
              node.name,
              node.content,
              7,
              "",
              "",
              "", "",
              this.UploadChecklist,
              this.UplaodBestPractice,
              this.EnterLanguageType,
              node.code, "",this.projectLifecycleData.projectID,
              signal
            ).pipe(takeUntil(cancel$))
          );
          console.log("response: ", response);
          node.codeReview = response;
          node.iscodeReviewGenerated = true; // Add flag
          this.generated = this.generated + 1;
          this.projectLifecycleData.codeReview = 'Completed';
          // await this.syncToBackend('codeReview Completed');
          // this.showCodeContent(node.codeReview);

        }
        node.expanded = true;
      }
      catch (error) {
        console.error(`Error during processing file: ${node.name}`, error);
      }
    }

    // Recursively traverse children
    if (node.children && !signal?.aborted) {
      for (const child of node.children) {
        await this.traverseAndUpdateFolderStructure(
          {
            node: child, level: level + 1, parentfolder, signal, cancel$ // ← propagate the signal
          });
        if (signal?.aborted) break;               // short-circuit if aborted
      }
    }
    console.log(`↰ finished ${node.name}`);
  }

  mandatory(item: any): boolean {
    if (item.name === "Code Review Summary") {
      return false
    }

    return MandatoryFolders.includes(item.name);
  }

  refreshSingleFolder(item: any) {

    if (!this.apicallvariable) {
      if (item.name === "UnitTest") {
        this.createUnittesttree(this.unitTesting);
        const UnitTest = {
          name: 'UnitTest',
          type: 'folder',
          expanded: true,
          children: this.unittestingtree,

        };

        const index = this.codeSynthesisFolderStructure[0]['children'][0]['children'].findIndex((item: any) => item.name === "UnitTest");

        if (index !== -1) {
          this.codeSynthesisFolderStructure[0]['children'][0]['children'][index] = UnitTest; // directly update the existing array
        }
        // this.codeSynthesisFolderStructure[0]['children'][0]['children'].push(UnitTest);

      }

      if (item.name === "DataScripting") {
        this.createdatabasetree(this.databaseScripts);
        const DataScripting = {
          name: 'DataScripting',
          type: 'folder',
          expanded: false,
          children: this.datascripttree,
        };
        const index = this.codeSynthesisFolderStructure[0]['children'][0]['children'].findIndex((item: any) => item.name === "DataScripting");

        if (index !== -1) {
          this.codeSynthesisFolderStructure[0]['children'][0]['children'][index] = DataScripting; // directly update the existing array
        }
      }

      if (item.name === "FunctionalTest") {
        this.createFunctiontesttree(this.FunctionalTesting);
        const FunctionalTest = {
          name: 'FunctionalTest',
          type: 'folder',
          expanded: false,
          children: this.functiontestingtree,
        };
        const index = this.codeSynthesisFolderStructure[0]['children'][0]['children'].findIndex((item: any) => item.name === "FunctionalTest");

        if (index !== -1) {
          this.codeSynthesisFolderStructure[0]['children'][0]['children'][index] = FunctionalTest; // directly update the existing array
        }
      }

      if (item.name === "IntegrationTest") {
        this.createIntegrationtesttree(this.IntegrationTesting);
        const IntegrationTest = {
          name: 'IntegrationTest',
          type: 'folder',
          expanded: false,
          children: this.Integrationtestingtree,
        };
        const index = this.codeSynthesisFolderStructure[0]['children'][0]['children'].findIndex((item: any) => item.name === "IntegrationTest");

        if (index !== -1) {
          this.codeSynthesisFolderStructure[0]['children'][0]['children'][index] = IntegrationTest; // directly update the existing array
        }
      }
      if (item.name === "Documentation") {


        this.templates = {
          hld: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.hldtemplate,
          lld: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.lldtemplate,
          userManual: "Solution Overview: \n" + this.solutionOverview + "\n\n" + "Solution Structure: \n" + this.projectStructureTemplate + "\n\n" + "Document Template: \n" + this.usermanualtemplate,
          traceabilityMatrix: this.TraceabilityMatrixtemplate
        };

        const documentationjson = [{
          name: 'HLD',
          type: 'file',
          expanded: true,
          content: this.templates['hld'],
        },
        {
          name: 'LLD',
          type: 'file',
          expanded: true,
          content: this.templates['lld'],
        },
        {
          name: 'User Manual',
          type: 'file',
          expanded: true,
          content: this.templates['userManual'],   //userManual
        },
        {
          name: 'Traceability matrix',
          type: 'file',
          expanded: true,
          content: this.templates['traceabilityMatrix'],   //userManual
        }]
        const Documentation = {
          name: 'Documentation',
          type: 'folder',
          expanded: false,
          children: documentationjson,
        };


        const index = this.codeSynthesisFolderStructure[0]['children'][0]['children'].findIndex((item: any) => item.name === "Documentation");

        if (index !== -1) {
          this.codeSynthesisFolderStructure[0]['children'][0]['children'][index] = Documentation; // directly update the existing array
        }
      }
    }
  }


  refreshFolderStructure() {
    if (!this.isAnalyzingCOD) {
      // this.codeSynthesisFolderStructure = this.folderStructure
      // this.fetchFolderStructure(this.projectStructure);
      this.createFunctiontesttree(this.FunctionalTesting);
      this.createUnittesttree(this.unitTesting);
      this.createIntegrationtesttree(this.IntegrationTesting);
      this.createdatabasetree(this.databaseScripts);
      this.synfetchFolderStructure();
    }
  }



  selected = 0;
  async checkselected(node: any, level: number = 0) {
    if (node.selected) {
      this.selected = this.selected + 1;
    }

    // Recursively traverse children
    if (node.children) {
      for (const child of node.children) {
        await this.checkselected(child, level + 1);
      }
    }
  }

  //#endregion







  /* --------------------------------------------------
     Download Final solution 
  -------------------------------------------------- */

  //#region downloadfolderstructure
  downloadZip() {
    this.downloadFolderStructure(this.codeSynthesisFolderStructure[0]);
  }
  public async downloadFolderStructure(folderStructure: any) {
    const zip = new JSZip();
    this.addFolderToZip(folderStructure, zip);
    const content = await zip.generateAsync({ type: 'blob' });
    saveAs(content, 'folder-structure.zip');
    // log
    this.apiService.logFrontend(`[INFO] ${'zip-file downloaded'}`).subscribe();
  }

  // private addFolderToZip(folder: any, zip: JSZip, folderPath: string = '') {
  //   if (folder.type === 'folder') {
  //     const newFolderPath = folderPath
  //       ? `${folderPath}/${folder.name}`
  //       : folder.name;
  //     folder.children.forEach((child: any) => {
  //       this.addFolderToZip(child, zip, newFolderPath);
  //     });
  //   } else if (folder.type === 'file') {
  //     // Add code file
  //     if (folder.code) {
  //       zip.file(`${folderPath}/${folder.name}`, folder.code);
  //     }

  //     // Add description file
  //     if (folder.description) {
  //       zip.file(`${folderPath}/${folder.name}.txt`, folder.description);
  //     }
  //   }
  // }

  private addFolderToZip(folder: any, zip: JSZip, folderPath: string = '') {
  if (folder.type === 'folder') {
    const newFolderPath = folderPath
      ? `${folderPath}/${folder.name}`
      : folder.name;
    folder.children.forEach((child: any) => {
      this.addFolderToZip(child, zip, newFolderPath);
    });
  } else if (folder.type === 'file') {
    let hasFile = false;
 
    // Add code file
    if (folder.code) {
      zip.file(`${folderPath}/${folder.name}`, folder.code);
      hasFile = true;
    }
 
    // Add description file
    if (folder.description) {
      zip.file(`${folderPath}/description_${folder.name}.txt`, folder.description);
      hasFile = true;
    }
 
    // Add codeReview file
    if (folder.codeReview) {
      const fileNameWithoutExt = folder.name;
      zip.file(`${folderPath}/codeReview_${fileNameWithoutExt}.txt`, folder.codeReview);
      hasFile = true;
    }
 
    // If no code/description/codeReview → save as .txt with content
    if (!hasFile && folder.content) {
      zip.file(`${folderPath}/${folder.name}.txt`, folder.content);
    }
  }
}
 
  //#endregion







  /* --------------------------------------------------
     Save and Upload Solution Logic
  -------------------------------------------------- */

  //#region Saved Solution
  async saveagentictext() {
    try {
      // Dynamic import JSZip
      const JSZip = await import('jszip');
      const zip = new JSZip.default();
      // main folder


      // subfolders

      //1. Insight Elicitation
      const insightelicitaion = zip.folder('InsightElicitation')!;
      // Add files
      insightelicitaion.file("UploadedBRD.txt", this.inputText || '');
      insightelicitaion.file('OptimizedFormat.txt', this.outputText || '');
      insightelicitaion.file('OptimizedFormat2.txt', this.outputText2 || '');
      insightelicitaion.file('step3.txt', this.outputText3 || '');
      insightelicitaion.file('step4.txt', this.outputText4 || '');


      //2. solidification
      const Solidification = zip.folder('Solidification')!;
      // Add files
      Solidification.file('Solidification.txt', this.technicalRequirement || '');


      //3. Blueprinting
      const blueprintingFolder = zip.folder('blueprintingFolder')!;
      // Add files
      blueprintingFolder.file('solutionOverview.txt', this.solutionOverview || '');
      blueprintingFolder.file('dataFlow.txt', this.dataFlow || '');
      blueprintingFolder.file('commonFunctionalities.txt', this.commonFunctionalities || '');
      blueprintingFolder.file('projectStructureDescription.txt', this.projectStructureDescription || '');
      blueprintingFolder.file('requirementSummary.txt', this.requirementSummary || '');
      blueprintingFolder.file('unitTesting.txt', this.unitTesting || '');
      blueprintingFolder.file('databaseScripts.txt', this.databaseScripts || '');
      blueprintingFolder.file('databaseScripts.txt', this.databaseScripts || '');

      // blueprintingFolder.file('projecttree.txt', this.folderStructure || "");

      if (this.folderStructure && Array.isArray(this.folderStructure)) {
        blueprintingFolder.file('folderStructure.json', JSON.stringify(this.folderStructure, null, 2));
      } else if (this.folderStructure) {
        // Fallback for non-array data
        blueprintingFolder.file('folderStructure.json', JSON.stringify(this.folderStructure, null, 2));
      }
      blueprintingFolder.file('UploadChecklist.txt', this.UploadChecklist || "");
      blueprintingFolder.file('EnterLanguageType.txt', this.EnterLanguageType || "");
      blueprintingFolder.file('UplaodBestPractice.txt', this.UplaodBestPractice || "");

      blueprintingFolder.file('FunctionalTesting.txt', this.FunctionalTesting || "");
      blueprintingFolder.file('IntegrationTesting.txt', this.IntegrationTesting || "");
      if(this.isGreenfield){
        blueprintingFolder.file('Greenbrown.txt', "Greenfield");
      }
      else if(this.isBrownfield){
        blueprintingFolder.file('Greenbrown.txt', "Brownfield");
      }

      blueprintingFolder.file('reverseEngineeringAlreadyCalled.txt',this.reverseEngineeringAlreadyCalled.toString());
      blueprintingFolder.file('projectLifecycleData.json',JSON.stringify(this.projectLifecycleData, null, 2));


      if (this.templates && Array.isArray(this.templates)) {
        blueprintingFolder.file('templates.json', JSON.stringify(this.templates, null, 2));
      } else if (this.templates) {
        // Fallback for non-array data
        blueprintingFolder.file('templates.json', JSON.stringify(this.templates, null, 2));
      }
      //4. Codesynthesis
      const CodesynthesisFolder = zip.folder('Codesynthesis')!;


      if (this.codeSynthesisFolderStructure && Array.isArray(this.codeSynthesisFolderStructure)) {
        CodesynthesisFolder.file('codeSynthesisFolderStructure.json', JSON.stringify(this.codeSynthesisFolderStructure, null, 2));
      } else if (this.codeSynthesisFolderStructure) {
        // Fallback for non-array data
        CodesynthesisFolder.file('codeSynthesisFolderStructure.json', JSON.stringify(this.codeSynthesisFolderStructure, null, 2));
      }



      // Generate and download
      const content = await zip.generateAsync({ type: 'blob' });
      const link = document.createElement('a');
      link.href = URL.createObjectURL(content);

      // Format date and time nicely
      const now = new Date();
      const pad = (n: number) => n.toString().padStart(2, '0');

      const year = now.getFullYear();
      const month = pad(now.getMonth() + 1);
      const day = pad(now.getDate());

      let hours = now.getHours();
      const minutes = pad(now.getMinutes());
      const ampm = hours >= 12 ? 'PM' : 'AM';
      hours = hours % 12 || 12;


      link.download = `CodeCraftAI-${year}-${month}-${day}_${pad(hours)}-${minutes}-${ampm}.zip`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      URL.revokeObjectURL(link.href);
    } catch (error) {
      console.error('Error creating zip file:', error);
    }
  }
isLoading = false;
async uploadagentic(event: any) {
  
  const file = event.target.files[0];

  if (!file || !file.name.endsWith('.zip')) {
    console.error('Please select a valid zip file');
    return;
  }

  // this.isLoading = true; // Ensure isLoading is set before any potential early returns
this.isLoading = true;
  try {
    const JSZip = await import('jszip');
    const zip = new JSZip.default();
    const zipContent = await zip.loadAsync(file);

    // Read all files
    await Promise.all([
      this.readBlueprinting(zipContent),
      this.readCodeSynthesis(zipContent),
      this.readInsightElicitation(zipContent),
      this.readSolidification(zipContent)
    ]);

    if (this.greenbrownfield) {
      if (this.greenbrownfield === 'Brownfield') {
        this.isBrownfield = true;
        this.isGreenfield = false;
      } else if (this.greenbrownfield === 'Greenfield') {
        this.isBrownfield = false;
        this.isGreenfield = true;
      }
    } else if (this.solutionOverview) {
      const value = this.extractFieldValue(this.solutionOverview);
      if (value === 'brown') {
        this.isBrownfield = true;
        this.isGreenfield = false;
      } else if (value === 'green') {
        this.isBrownfield = false;
        this.isGreenfield = true;
      }
    } else {
      if (this.technicalRequirement === '') {
        this.isBrownfield = true;
        this.isGreenfield = false;
      } else {
        this.isBrownfield = false;
        this.isGreenfield = true;
      }
    }

    await this.customerfunction(this.codeSynthesisFolderStructure[0], 0, '');
  } catch (error) {
    console.error('Error processing agentic:', error);
  } finally {
    this.isLoading = false; // Ensure isLoading is reset in the finally block
  }

}

greenbrownfield = ""
  // Helper method to read Documentation files
  // Helper method to read Documentation files
  private async readBlueprinting(zipContent: any) {



    try {
      const solutionFile = zipContent.file('blueprintingFolder/solutionOverview.txt');
      if (solutionFile) {
        this.solutionOverview = await solutionFile.async('text');
      }

      const dataFlow = zipContent.file('blueprintingFolder/dataFlow.txt');
      if (dataFlow) {
        this.dataFlow = await dataFlow.async('text');
      }

      const commonFunctionalities = zipContent.file('blueprintingFolder/commonFunctionalities.txt');
      if (commonFunctionalities) {
        this.commonFunctionalities = await commonFunctionalities.async('text');
      }

      const greenbrown = zipContent.file('blueprintingFolder/Greenbrown.txt');
      if (greenbrown) {
        this.greenbrownfield = await greenbrown.async('text');
        }



      const requirementSummary = zipContent.file('blueprintingFolder/requirementSummary.txt');
      if (requirementSummary) {
        this.requirementSummary = await requirementSummary.async('text');
      }

      const unitTesting = zipContent.file('blueprintingFolder/unitTesting.txt');
      if (unitTesting) {
        this.unitTesting = await unitTesting.async('text');
        this.unitTesting = this.cleanUnitTestTextAdvanced(this.unitTesting);
        this.createUnittesttree(this.unitTesting);

      }

      const databaseScripts = zipContent.file('blueprintingFolder/databaseScripts.txt');
      if (databaseScripts) {
        this.databaseScripts = await databaseScripts.async('text');
        this.createdatabasetree(this.databaseScripts);

      }
      const reverseEngineeringAlreadyCalled = zipContent.file('blueprintingFolder/reverseEngineeringAlreadyCalled.txt');
      if (reverseEngineeringAlreadyCalled) {
        const reverseEngineeringAlreadyCalledTEMP = await reverseEngineeringAlreadyCalled.async('text');
        if (reverseEngineeringAlreadyCalledTEMP === "true"){
          this.reverseEngineeringAlreadyCalled = true;
        }
        else{
          this.reverseEngineeringAlreadyCalled = false;
        }
        console.log("successfully loaded reverseengineering variable")
        console.log("reverseEngineeringAlreadyCalled",this.reverseEngineeringAlreadyCalled.toString())

      }




      // const projecttree = zipContent.file('blueprintingFolder/projectStructureDescription.txt');
      // if (projecttree) {
      //   this.projectStructureDescription = await projecttree.async('text');
      //   //console.log(this.projectStructureDescription);
      //   this.projectStructure = await projecttree.async('text');
      //   this.fetchFolderStructure(this.projectStructureDescription);

      // }

      const projecttree = zipContent.file('blueprintingFolder/projectStructureDescription.txt');
      if (projecttree) {
        this.projectStructureDescription = await projecttree.async('text');
        this.projectStructure = await projecttree.async('text');
        const filePath = 'blueprintingFolder/folderStructure.json';
        const structureFile = zipContent.file(filePath);
        if (!structureFile) {

          this.fetchFolderStructure(this.projectStructureDescription);
        }
        else {
          try {
            const jsonString = await structureFile.async('text');
            let folderStructure;
            try {
              folderStructure = JSON.parse(jsonString);
            } catch (parseError) {
              console.error('Error parsing project structure JSON:', parseError);
              this.folderStructure = [];
              return;
            }

            // Type check and assign
            if (Array.isArray(folderStructure)) {
              this.folderStructure = folderStructure;
            } else if (folderStructure) {
              this.folderStructure = [folderStructure];
            } else {
              this.folderStructure = [];
            }
          } catch (error) {
            console.error('Error reading file:', error);
            this.folderStructure = [];
          }
        }
        this.folderStructureboolean = true;


      }


      const IntegrationTesting = zipContent.file('blueprintingFolder/IntegrationTesting.txt');
      if (IntegrationTesting) {
        this.IntegrationTesting = await IntegrationTesting.async('text');
        this.IntegrationTesting = this.cleanUnitTestTextAdvanced(this.IntegrationTesting);
        this.createIntegrationtesttree(this.IntegrationTesting);

      }

      const FunctionalTesting = zipContent.file('blueprintingFolder/FunctionalTesting.txt');
      if (projecttree) {
        this.FunctionalTesting = await FunctionalTesting.async('text');
        this.FunctionalTesting = this.cleanUnitTestTextAdvanced(this.FunctionalTesting);
        this.createFunctiontesttree(this.FunctionalTesting);
      }

      const UploadChecklist = zipContent.file('blueprintingFolder/UploadChecklist.txt');
      if (UploadChecklist) {
        this.UploadChecklist = await UploadChecklist.async('text');
      }

      const EnterLanguageType = zipContent.file('blueprintingFolder/EnterLanguageType.txt');
      if (EnterLanguageType) {
        this.EnterLanguageType = await EnterLanguageType.async('text');
      }
      const UplaodBestPractice = zipContent.file('blueprintingFolder/UplaodBestPractice.txt');
      if (UplaodBestPractice) {
        this.UplaodBestPractice = await UplaodBestPractice.async('text');
      }


      const templates = zipContent.file('blueprintingFolder/templates.json');
      if (templates) {
        const content = await templates.async('string');
        this.templates = JSON.parse(content);
      }

      const projectLifecycle = zipContent.file('blueprintingFolder/projectLifecycleData.json');
      if (projectLifecycle) {
        const projectLifecycleData = await projectLifecycle.async('string');
        this.projectLifecycleData = JSON.parse(projectLifecycleData);

      }

    } catch (error) {
      console.warn('Error reading documentation files:', error);
    }
  }
  // Helper method to read Technical files
  private async readCodeSynthesis(zipContent: any) {
    const filePath = 'Codesynthesis/codeSynthesisFolderStructure.json';
    const structureFile = zipContent.file(filePath);
    if (!structureFile) {
      console.error(`File not found: ${filePath}`);
      this.codeSynthesisFolderStructure = [];
      return;
    }

    try {
      const jsonString = await structureFile.async('text');
      let codeSynthesisFolderStructure;
      try {
        codeSynthesisFolderStructure = JSON.parse(jsonString);
      } catch (parseError) {
        console.error('Error parsing project structure JSON:', parseError);
        this.codeSynthesisFolderStructure = [];
        return;
      }

      // Type check and assign
      if (Array.isArray(codeSynthesisFolderStructure)) {
        this.codeSynthesisFolderStructure = codeSynthesisFolderStructure;
      } else if (codeSynthesisFolderStructure) {
        this.codeSynthesisFolderStructure = [codeSynthesisFolderStructure];
      } else {
        this.codeSynthesisFolderStructure = [];
      }
    } catch (error) {
      console.error('Error reading file:', error);
      this.codeSynthesisFolderStructure = [];
    }
    this.codeSynthesisFolderStructureboolean = true
  }

  // Helper method to read Testing files
  private async readInsightElicitation(zipContent: any) {
    try {
      const UploadedBRD = zipContent.file('InsightElicitation/UploadedBRD.txt');
      if (UploadedBRD) {
        this.inputText = await UploadedBRD.async('text');

      }

      const OptimizedFormat = zipContent.file('InsightElicitation/OptimizedFormat.txt');
      if (OptimizedFormat) {
        this.outputText = await OptimizedFormat.async('text');

      } const OptimizedFormat2 = zipContent.file('InsightElicitation/OptimizedFormat2.txt');
      if (OptimizedFormat2) {
        this.outputText2 = await OptimizedFormat2.async('text');

      }
      const step3 = zipContent.file('InsightElicitation/step3.txt');
      if (step3) {
        this.outputText3 = await step3.async('text');

      }
      const step4 = zipContent.file('InsightElicitation/step4.txt');
      if (step4) {
        this.outputText4 = await step4.async('text');

      }
    } catch (error) {
      console.warn('Error reading testing files:', error);
    }
  }

  // Helper method to read Database files
  private async readSolidification(zipContent: any) {
    try {
      const Solidification = zipContent.file('Solidification/Solidification.txt');
      if (Solidification) {
        this.technicalRequirement = await Solidification.async('text');
      }
    } catch (error) {
      console.warn('Error reading database files:', error);
    }
  }



  extractSolutionStructure(text: string): string {
    if (!text) {
      return "";
    }
    // Split by 'Solution Structure:' (case-insensitive, with or without prefix)
    const splitText = text.split(/solution structure:/i);
    const contentBelowSolutionStructure = (splitText.length > 1)
      ? splitText[1].trim()
      : '';
    return contentBelowSolutionStructure;
  }


  /* give every node a 'selected' flag the first time */
  private initialiseSelection(nodes: any[]): void {
    nodes?.forEach((n: any) => {
      if (n.selected === undefined) { n.selected = false; }
      if (n.children?.length) { this.initialiseSelection(n.children); }
    });
  }

  extractSolutionOverviewSection(inputText: string): string {
    if (!inputText) return '';

    const lines = inputText.split(/\r?\n/);
    let capturing = false;
    let resultLines: string[] = [];

    for (const line of lines) {
      const trimmed = line.trim().toLowerCase();

      // Detect start of section (e.g., "solution overview:" in any casing)
      if (/^solution overview[:：]?$/.test(trimmed)) {
        capturing = true;
        continue; // skip heading line
      }

      // Detect end of section (e.g., "technical requirements:")
      if (/^solution structure[:：]?$/.test(trimmed)) {
        break;
      }

      if (capturing) {
        resultLines.push(line);
      }
    }

    return resultLines.join('\n').trim();
  }

  //#endregion








  /* --------------------------------------------------
     checkbox helpers
  -------------------------------------------------- */

  //#region checkbox helpers

  selectTreeNode(item: any) {

    this.selectedTreeNode = item;

    // Optional: Scroll to selected element

    setTimeout(() => {

      const selectedElement = document.querySelector('.tree-node span.selected');

      if (selectedElement) {

        selectedElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

      }

    }, 100);

  }


  // Method to select tree node in Code Synthesis

  selectCodeTreeNode(item: any, type: string = '') {

    this.selectedCodeTreeNode = item;

    this.selectedCodeTreeNodeType = type;

  }

  // Optional: Method to clear selection

  clearTreeSelection() {

    this.selectedTreeNode = null;

    this.selectedCodeTreeNode = null;

    this.selectedCodeTreeNodeType = '';

  }


  onSelectionChange(node: any): void {
    /* tick / untick all descendants if user clicked a folder */
    if (node.type === 'folder') {
      this.setSelectionRecursively(node, node.selected);
    }
  }

  private setSelectionRecursively(node: any, isChecked: boolean): void {
    node.children?.forEach((child: any) => {
      child.selected = isChecked;
      if (child.type === 'folder') {
        this.setSelectionRecursively(child, isChecked);
      }
    });
  }

  /* does a folder (recursively) contain something selected? */
  private folderHasSelection(node: any): boolean {
    if (node.selected) { return true; }
    return node.children?.some((c: any) => this.folderHasSelection(c)) ?? false;
  }

  //#endregion



}