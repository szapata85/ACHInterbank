import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { AchColombiaFileExchangeService, ManagedMftAdministration } from '../ach-colombia-file-exchange/ach-colombia-file-exchange.service';

@Component({ selector: 'app-ach-colombia-mft-administration', standalone: true, imports: [CommonModule, FormsModule], template: `
<section class="container py-3" *ngIf="model"><h2>Administración MFT ACH</h2>
<p class="text-danger" *ngIf="error">{{error}}</p><fieldset [disabled]="!canManage || busy">
<label>Nombre <input class="form-control" [(ngModel)]="model.profileName"></label><label>Proveedor <input class="form-control" [(ngModel)]="model.provider"></label>
<label>Protocolo <input class="form-control" [(ngModel)]="model.protocol"></label><label>Endpoint <input class="form-control" [(ngModel)]="model.endpoint"></label>
<label>Puerto <input class="form-control" type="number" [(ngModel)]="model.port"></label><label>Usuario / identificador seguro <input class="form-control" [(ngModel)]="model.principal"></label>
<label><input type="checkbox" [(ngModel)]="model.profileEnabled"> Perfil habilitado</label><hr><h4>Rutas operativas</h4>
<label>Ruta de entrega de salida <input class="form-control" [(ngModel)]="model.outboundLocation"></label><label>Ruta de recepción de entrada <input class="form-control" [(ngModel)]="model.inboundLocation"></label><label>Ruta de archivo <input class="form-control" [(ngModel)]="model.archiveLocation"></label>
<h4>Reintento y retención</h4><label>Máximo de intentos <input class="form-control" type="number" [(ngModel)]="model.maximumRetries"></label><label>Demora (segundos) <input class="form-control" type="number" [(ngModel)]="model.retryDelaySeconds"></label><label>Retención (días) <input class="form-control" type="number" [(ngModel)]="model.retentionDays"></label>
<button class="btn btn-primary mt-3" *ngIf="canManage" (click)="save()">Guardar configuración</button></fieldset>
<hr><h4>Credencial</h4><p>{{model.credentialConfigured ? 'Configurada' : 'No configurada'}} <span *ngIf="model.credentialUpdatedAtUtc">— Última actualización: {{model.credentialUpdatedAtUtc | date:'short'}}</span></p>
<div *ngIf="canManage"><label>Tipo <input class="form-control" [(ngModel)]="credentialType"></label><label>Credencial <input class="form-control" type="password" autocomplete="new-password" [(ngModel)]="secret"></label><button class="btn btn-outline-primary mt-2" [disabled]="busy" (click)="rotate()">{{model.credentialConfigured ? 'Rotar credencial' : 'Configurar credencial'}}</button></div></section>`, styles: ['label{display:block;margin:.5rem 0} input{max-width:520px}'] })
export class AchColombiaMftAdministrationComponent implements OnInit {
  private readonly api = inject(AchColombiaFileExchangeService); private readonly auth = inject(AuthService); private readonly notifications = inject(NotificationService);
  readonly canManage = this.auth.hasPermission('CanManageAch'); model?: ManagedMftAdministration; secret = ''; credentialType = 'Password'; busy = false; error = '';
  ngOnInit(): void { this.load(); }
  load(): void { this.busy = true; this.api.administration().pipe(finalize(() => this.busy = false)).subscribe({ next: x => this.model = x, error: e => this.error = e?.error?.detail ?? 'No fue posible cargar la configuración.' }); }
  save(): void { if (!this.model || !this.canManage) return; this.run(this.api.updateAdministration(this.model)); }
  rotate(): void { if (!this.model || !this.canManage || !this.secret.trim()) return; this.busy = true; this.api.setCredential(this.credentialType, this.secret).pipe(finalize(() => this.busy = false)).subscribe({ next: x => { this.model = x; this.secret = ''; this.notifications.success('Credencial protegida actualizada.'); }, error: e => this.error = e?.error?.detail ?? 'No fue posible actualizar la credencial.' }); }
  private run(request: ReturnType<AchColombiaFileExchangeService['updateAdministration']>): void { this.busy = true; request.pipe(finalize(() => this.busy = false)).subscribe({ next: x => { this.model = x; this.notifications.success('Configuración actualizada.'); }, error: e => this.error = e?.error?.detail ?? 'No fue posible guardar la configuración.' }); }
}
