import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class PaginationService {
  private pageSize:number=20
  constructor() {
    
   }

   public getPageSize()
   {
    return this.pageSize
   }

   public setPageSizes(size:number)
   {
    this.pageSize=size
   }
}
