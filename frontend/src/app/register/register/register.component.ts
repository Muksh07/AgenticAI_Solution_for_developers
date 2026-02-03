import { Component, OnInit } from '@angular/core';
import { ApiService, IUser } from '../../../Services/api.service';
import { Router, RouterLink } from '@angular/router';
import { FormsModule, NgForm } from '@angular/forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports:[FormsModule,RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {

  user: IUser = { 
    email: '',
    password: '',
    status: 'false'
  };

  constructor(private apiService: ApiService,private routes: Router ) { }

  ngOnInit() {
  }

   onSubmit(registrationForm: NgForm) 
  {
    if(registrationForm.valid)
    {
      this.apiService.registerUser(this.user).subscribe(
        response => {
          console.log('User registered successfully:', response);
          // this.alertify.success('Registration successful! Please wait for admin approval.')
          registrationForm.resetForm();
            // Clear the form data object
            this.user = {
              email: '',
              password: '',
            }
        },
        error => {
          // this.alertify.error('Email already Exist')
          console.error('Error registering user:', error);
        }
      );

    }
  }

}
