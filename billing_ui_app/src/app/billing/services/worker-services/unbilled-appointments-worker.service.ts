import { Injectable } from "@angular/core";

@Injectable()
export class UnbilledAppointmentsWorkerService {
    private worker: Worker | null = null;

    constructor(){
      // Initialize the worker if it's not already running
      this.worker = new Worker(new URL('./unbilled-appointments.worker', import.meta.url), { type: 'module' });
    }
  
    // Method to get the worker instance
    getWorker(): Worker {
      return this.worker!;
    }
  
    // Terminate the worker when no longer needed
    terminateWorker(): void {
      if (this.worker) {
        this.worker.terminate();
        this.worker = null;
      }
    }
  
    ngOnDestroy(): void {
      this.terminateWorker();
    }
}