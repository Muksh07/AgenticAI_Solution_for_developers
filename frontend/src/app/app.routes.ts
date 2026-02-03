import { Routes } from '@angular/router';
import { AppComponent } from './app.component';
import { LoginComponent } from './login/login/login.component';
import { RegisterComponent } from './register/register/register.component';

export const routes: Routes = [
    {path: '', component: AppComponent},  //LoginComponent
    {path: 'login', component: LoginComponent},
    {path: 'sign-up', component: RegisterComponent},
];
