import { Routes } from '@angular/router';
import { FunPageComponent } from './fun-page.component';
import { HomeComponent } from './home.component';
import { ResultPageComponent } from './result-page.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'q/:id', component: ResultPageComponent },
  { path: 'fun', component: FunPageComponent },
  { path: 'fun/:id', component: FunPageComponent },
  { path: '**', redirectTo: '' }
];
