import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import {environment} from '../../environments/environment'
import { ProjectFeedback, ProjectLifecycle } from '../app/app.component';
// import { ReverseEngineeringRequestModel } from '../app/app.component';

@Injectable({
  providedIn: 'root',
})
export class ApiService {

  hldText: string = '';
  constructor(private http: HttpClient) {}
  
  baseUrl = environment.BaseUrl;
  private apiUrl = this.baseUrl+'/BRDAnalyzer/analyse';
  private apiurlsolidify = this.baseUrl+'/BRDAnalyzer/solidify';
  private apiurlBlueprinting = this.baseUrl+'/BRDAnalyzer/BluePrinting';
  private apiurlCodesynthesis = this.baseUrl+'/BRDAnalyzer/CodeSyn'; 
  private apiurllogin = this.baseUrl+'/User/LoginAccount';
  private apiurlsignup = this.baseUrl+'/User/CreateUserAccount';
  private apireverseEngineering = this.baseUrl+'/BRDAnalyzer/reverse';
  private apigetcodereview = this.baseUrl+'/BRDAnalyzer/getsummary';
  private apiTraceability = this.baseUrl+'/BRDAnalyzer/Traceability';


  // submitProjectFeedback(feedbackData: ProjectFeedback): Observable<any> {
  //   return this.http.post<any>(`${this.baseUrl}/Database/submitFeedback`, feedbackData)

  // }

  /**
   * Get all feedbacks for a specific project
   */
  // getProjectFeedbacks(projectId: number): Observable<ProjectFeedback[]> {
  //   return this.http.get<ProjectFeedback[]>(`${this.baseUrl}/api/projectfeedback/project/${projectId}`)

  // }

  /**
   * Update existing feedback
   */
  // updateProjectFeedback(feedbackId: number, feedbackData: Partial<ProjectFeedback>): Observable<any> {
  //   return this.http.put<any>(`${this.baseUrl}/api/projectfeedback/${feedbackId}`, feedbackData)

  // }

  /**
   * Delete a feedback entry
   */
  // deleteProjectFeedback(feedbackId: number): Observable<any> {
  //   return this.http.delete(`${this.baseUrl}/api/projectfeedback/${feedbackId}`)

  // }

//  saveProjectLifecycle(projectData: ProjectLifecycle): Observable<any> {
//     return this.http.post(`${this.baseUrl}/Database/createproject`, projectData);
//   }

  // updateProjectLifecycle(projectId: number, projectData: ProjectLifecycle): Observable<any> {
  //   return this.http.put(`${this.baseUrl}/Database/${projectId}`, projectData);
  // }

  // getProjectLifecycle(projectId: number): Observable<ProjectLifecycle> {
  //   return this.http.get<ProjectLifecycle>(`${this.baseUrl}/Database/${projectId}`);
  // }

  // Convert observables to promises for async/await usage
  // saveProjectLifecycleAsync(projectData: ProjectLifecycle): Promise<any> {
  //   return this.saveProjectLifecycle(projectData).toPromise();
  // }

  // updateProjectLifecycleAsync(projectId: number, projectData: ProjectLifecycle): Promise<any> {
  //   return this.updateProjectLifecycle(projectId, projectData).toPromise();
  // }

  APIanalyzeBRD(context: string,brdContent: string,task: string,stepno:number, id: number): Observable<string> 
  {
    const requestBody = {
      context: context || '',
      BRDContent: brdContent,
      task: task || '',
      stepno:stepno,
      id: id
    };
    return this.http.post<string>(this.apiUrl, requestBody, {
      responseType: 'text' as 'json',
    });
  }

  Solidify(fromInsightContent: string, id: number, stepno: number): Observable<string> {
    const requestBody = {
      //context: context || '',
      AnalysisResult: fromInsightContent,
      id: id,
      stepno:stepno
    };
    return this.http.post<string>(this.apiurlsolidify, requestBody, {
      responseType: 'text' as 'json',
    });
  }


  ReverseEngineering(data: any): Observable<any> {
    return this.http.post(this.apireverseEngineering, data);
  }

  getcodereviewSummary(data: any): Observable<string> {
    return this.http.post(this.apigetcodereview, data , { responseType: 'text'});
  }

   Traceability(data: any,requirementSummary: string ,field:string|null, id: number): Observable<string> {
    const payload = {
    fileNode: data,
    requirementSummary: requirementSummary,
    field:field,
    id:id
  };
    return this.http.post(this.apiTraceability, payload , { responseType: 'text'});
  }


  Blueprinting(
    fromSolidificationContent: string,selectedTabs: string[],id: number
  ): Observable<{ [key: string]: string }> {
    const requestBody = {
      SolidificationOutput: fromSolidificationContent,
      selectedTabs: selectedTabs,
      id:id
    };
    return this.http.post<{ [key: string]: string }>(
      this.apiurlBlueprinting,
      requestBody,
      { responseType: 'json' }
    );
  }
   
  getHldTemplate(): Observable<string> {
    return this.http.get('assets/HLD.txt', { responseType: 'text' });
  }

   getLldTemplate(): Observable<string> {
    return this.http.get('assets/LLD.txt', { responseType: 'text' });
  }

  getUserManualTemplate(): Observable<string> {
    return this.http.get('assets/UserManual.txt', { responseType: 'text' });
  }
  getTraceabilityMatrixTemplate(): Observable<string> {
    return this.http.get('assets/TraceabilityMatrix.txt', { responseType: 'text' });
  }

  Codesynthesis(Filename: string, Filecontent:string, k:number,DataFlow:string,solutionOver:string , commonFunctionalities:string , requirementSummary:string, UploadChecklist:string, UploadBestPractice:string, EnterLanguageType:string, code:string,description:string,id: number, signal?: AbortSignal  ): Observable<string>
  {
    const requestBody = {
      Filename: Filename,
      FileContent: Filecontent,
      i: k,
      DataFlow:DataFlow,
      SolutionOverview:solutionOver,
      commonFunctionalities:commonFunctionalities,
      requirementSummary:requirementSummary,

      UploadChecklist:UploadChecklist,
      UploadBestPractice:UploadBestPractice,
      EnterLanguageType:EnterLanguageType,
      code:code,
      description:description,
      id:id
    };
    const options: {
      responseType: 'text';
      signal?: AbortSignal;                // property exists only in Angular 15+
    } = {
      responseType: 'text'
    };
 
    if (signal) {
      // add the AbortSignal ONLY when caller supplied one
      options.signal = signal as any;      // “as any” avoids compilation errors on ≤ v14
    }
 
    console.log('Codesynthesis →', requestBody);
 
    // final cast gives us a clean Observable<string>
    return this.http.post(
      this.apiurlCodesynthesis,
      requestBody,
      options
    ) as Observable<string>;
  }
 

  

  logFrontend(message: string): Observable<any> {
  const url = this.baseUrl + '/BRDAnalyzer/log';
  return this.http.post(url, { message }, {
    headers: { 'Content-Type': 'application/json' }
  });
}
  
  login(email:string,password:string){
    const loginData = { email, password };
    return this.http.post<any>(this.apiurllogin, loginData);
  }

  registerUser(user: IUser): Observable<any> 
  {
      return this.http.post<any>(this.apiurlsignup, user);
  } 

  

  
}

export interface IUser 
{
  email: string;
  password: string;
  status?: string; 
}
