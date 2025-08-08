import * as signalR from '@microsoft/signalr';

export type InvestigationEvents = {
  onStarted?: (payload: { investigatorId: string; timestamp: string }) => void;
  onCompleted?: (payload: { investigatorId: string; resultCount: number; timestamp: string }) => void;
  onNewResult?: (payload: { investigatorId: string; result: any }) => void;
  onStatusChanged?: (payload: { investigatorId: string; newStatus: string }) => void;
  onConnectionChange?: (state: 'connecting' | 'connected' | 'disconnected') => void;
};

export class SignalRService {
  private connection: signalR.HubConnection | null = null;

  async start(baseUrl: string, handlers: InvestigationEvents): Promise<void> {
    if (this.connection) return;

    handlers.onConnectionChange?.('connecting');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/hubs/investigations`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('InvestigationStarted', (p) => handlers.onStarted?.(p));
    this.connection.on('InvestigationCompleted', (p) => handlers.onCompleted?.(p));
    this.connection.on('NewResultAdded', (p) => handlers.onNewResult?.(p));
    this.connection.on('StatusChanged', (p) => handlers.onStatusChanged?.(p));

    this.connection.onreconnecting(() => handlers.onConnectionChange?.('connecting'));
    this.connection.onreconnected(() => handlers.onConnectionChange?.('connected'));
    this.connection.onclose(() => handlers.onConnectionChange?.('disconnected'));

    await this.connection.start();
    handlers.onConnectionChange?.('connected');
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }
}


