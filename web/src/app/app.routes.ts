import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { ResultPageComponent } from './result-page.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'q/:id', component: ResultPageComponent },
  { path: '**', redirectTo: '' }
];
