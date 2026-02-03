// // feedback-form/feedback-form.component.ts
// import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
// import { FormBuilder, FormGroup, Validators } from '@angular/forms';
// import { CommonModule } from '@angular/common';
// import { ReactiveFormsModule } from '@angular/forms';
// import { ApiService } from '../../Services/api.service';

// export interface ProjectFeedback {
//   feedbackID?: number;
//   projectID: number;
//   codeCoverageScore?: number;
//   codeQualityScore?: number;
//   HLD?: string;
//   LLD?: string;
//   User_Manual?: string;
//   Traceability?: string;
//   reviewerComments?: string;
//   feedbackDate?: Date;
// }

// @Component({
//   selector: 'app-feedback-form',
//   standalone: true,
//   imports: [CommonModule, ReactiveFormsModule],
//   templateUrl: './feedback-form.component.html',
//   styleUrls: ['./feedback-form.component.css']
// })
// export class FeedbackFormComponent implements OnInit {
//   @Input() isVisible: boolean = false;
//   @Input() projectId: number = 0;
//   @Input() projectName: string = '';
//   @Output() closeForm = new EventEmitter<void>();
//   @Output() feedbackSubmitted = new EventEmitter<ProjectFeedback>();

//   feedbackForm: FormGroup;
//   isSubmitting: boolean = false;
//   existingFeedbacks: ProjectFeedback[] = [];

//   // Add these new dropdown options
//   coverageOptions = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
  
//   qualityScoreOptions = [1, 2, 3 ,4, 5];

//   qualityOptions = [
//     { value: 'Not Generated Yet', label: 'Not Generated Yet' },
//     { value: 'Excellent', label: 'Excellent - Outstanding quality' },
//     { value: 'Good', label: 'Good - Meets standards' },
//     { value: 'Average', label: 'Average - Acceptable quality' },
//     { value: 'Below Average', label: 'Below Average - Needs improvement' },
//     { value: 'Poor', label: 'Poor - Major issues' }
//   ];

//   constructor(
//     private fb: FormBuilder,
//     private apiService: ApiService
//   ) {
//     this.feedbackForm = this.fb.group({
//       codeCoverageScore: ['', [Validators.min(10), Validators.max(100)]],
//       codeQualityScore: ['', [Validators.min(1), Validators.max(5)]],
//       HLD: [''],
//       LLD: [''],
//       User_Manual: [''],
//       Traceability: [''],
//       reviewerComments: ['', [Validators.required, Validators.minLength(10)]]
//     });
//   }

//   ngOnInit() {
//     // if (this.projectId > 0) {
//     //   this.loadExistingFeedbacks();
//     // }
//   }

//   // loadExistingFeedbacks() {
//   //   this.apiService.getProjectFeedbacks(this.projectId).subscribe({
//   //     next: (feedbacks:any) => {
//   //       this.existingFeedbacks = feedbacks;
//   //     },
//   //     error: (error: any) => {
//   //       console.error('Error loading feedbacks:', error);
//   //     }
//   //   });
//   // }

//   //   showSuccessOverlay: boolean = false;
//   // successMessage: string = '';
//   onSubmit() {
//     if (this.feedbackForm.valid && this.projectId > 0) {
//       this.isSubmitting = true;

//       const feedbackData: ProjectFeedback = {
//         projectID: this.projectId,
//         codeCoverageScore: this.feedbackForm.value.codeCoverageScore || null,
//         codeQualityScore: this.feedbackForm.value.codeQualityScore || null,
//         HLD: this.feedbackForm.value.HLD,
//         LLD: this.feedbackForm.value.LLD,
//         User_Manual: this.feedbackForm.value.User_Manual,
//         Traceability: this.feedbackForm.value.Traceability,
        
//         reviewerComments: this.feedbackForm.value.reviewerComments,
//         feedbackDate: new Date()
//       };

//       this.apiService.submitProjectFeedback(feedbackData).subscribe({
//         next: (response: any) => {
//           this.isSubmitting = false;
//           this.feedbackSubmitted.emit(response);
//           this.resetForm();
//            this.showSuccessMessage('🎉 Feedback submitted successfully!');
//           console.log('✅ Feedback submitted successfully');
//         },
//         error: (error: any) => {
//           this.isSubmitting = false;
//            this.showSuccessMessage('❌ Error submitting feedback. Please try again.', true);
//           console.error('❌ Error submitting feedback:', error);
//         }
//       });
//     }
//   }

// showSuccessOverlay: boolean = false;
// successMessage: string = '';
// successTimer: any = null; // new property

// // ... existing code ...

// showSuccessMessage(message: string, isError: boolean = false) {
//   this.successMessage = message;
//   this.showSuccessOverlay = true;

//   // Clear any previous timer to avoid overlap
//   if (this.successTimer) {
//     clearTimeout(this.successTimer);
//     this.successTimer = null;
//   }

//   this.successTimer = setTimeout(() => {
//     this.showSuccessOverlay = false;
//     this.successTimer = null;
//     if (!isError) {
//       this.closeModal();
//     }
//   }, 2000);
// }

//   resetForm() {
//     this.feedbackForm.reset();
//     this.feedbackForm.patchValue({
//       artifactQuality: '',
//       reviewerComments: ''
//     });
//   }

//   closeModal() {
//     this.resetForm();
//     this.closeForm.emit();
//   }

//   getScoreColor(score: number): string {
//     if (score >= 90) return '#4CAF50';
//     if (score >= 80) return '#8BC34A';
//     if (score >= 70) return '#FFC107';
//     if (score >= 60) return '#FF9800';
//     return '#F44336';
//   }

//   getQualityColor(quality: string): string {
//     switch (quality) {
//       case 'Excellent': return '#4CAF50';
//       case 'Good': return '#8BC34A';
//       case 'Average': return '#FFC107';
//       case 'Below Average': return '#FF9800';
//       case 'Poor': return '#F44336';
//       default: return '#9E9E9E';
//     }
//   }
// }
