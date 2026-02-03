import { Component, OnInit } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ApiService } from '../../../Services/api.service';
import { Route, Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports:[FormsModule,RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  email: string = '';
  password: string = '';
  errorMessage: string = '';

  constructor(private apiService: ApiService,private routes: Router ) { }

  ngOnInit() {
  }

  onLogin(loginForm: NgForm): void 
  {
    if (loginForm.valid) {  
      const { email, password } = loginForm.value;
      this.apiService.login(email, password).subscribe(
        response => 
        {
          if (response) 
          {
            //this.alertify.success('Login successfull')
            this.routes.navigate(['']);
          } 
          else 
          {
            this.routes.navigate(['login']);
            //this.alertify.error('Wrong user or password');
          }
        },
        error => {
          //this.alertify.error('Wrong user or password');
          console.log('Login failed:', error);
        }
      );
    } else {
      //this.alertify.error('Wrong user or password');
    }
  }

}
