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
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      // Already connected; avoid duplicate starts
      // eslint-disable-next-line no-console
      console.log('🔍 SignalR: Connection already established, skipping start.');
      return;
    }

    handlers.onConnectionChange?.('connecting');

    const hubUrl = `${baseUrl.replace(/\/$/, '')}/hubs/investigations`;
    // eslint-disable-next-line no-console
    console.log('🚀 SignalR: Starting connection to', hubUrl);

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Tune keepalive/timeouts for dev environments
    this.connection.serverTimeoutInMilliseconds = 60000; // 60s
    this.connection.keepAliveIntervalInMilliseconds = 15000; // 15s

    this.connection.on('InvestigationStarted', (p) => {
      // eslint-disable-next-line no-console
      console.log('📢 SignalR: InvestigationStarted', p);
      handlers.onStarted?.(p);
    });
    this.connection.on('InvestigationCompleted', (p) => {
      // eslint-disable-next-line no-console
      console.log('📢 SignalR: InvestigationCompleted', p);
      handlers.onCompleted?.(p);
    });
    this.connection.on('NewResultAdded', (p) => {
      // eslint-disable-next-line no-console
      console.log('📢 SignalR: NewResultAdded', p);
      handlers.onNewResult?.(p);
    });
    this.connection.on('StatusChanged', (p) => {
      // eslint-disable-next-line no-console
      console.log('📢 SignalR: StatusChanged', p);
      handlers.onStatusChanged?.(p);
    });

    this.connection.onreconnecting(() => {
      // eslint-disable-next-line no-console
      console.log('🔄 SignalR: Reconnecting...');
      handlers.onConnectionChange?.('connecting');
    });
    this.connection.onreconnected(() => {
      // eslint-disable-next-line no-console
      console.log('✅ SignalR: Reconnected');
      handlers.onConnectionChange?.('connected');
    });
    this.connection.onclose((err) => {
      // eslint-disable-next-line no-console
      console.log('❌ SignalR: Connection closed', err);
      handlers.onConnectionChange?.('disconnected');
    });

    try {
      // eslint-disable-next-line no-console
      console.log('🔌 SignalR: Attempting connection...');
      await this.connection.start();
      // eslint-disable-next-line no-console
      console.log('✅ SignalR: Connection successful!');
      handlers.onConnectionChange?.('connected');
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error('❌ SignalR: Connection failed:', error);
      handlers.onConnectionChange?.('disconnected');
      try { await this.connection.stop(); } catch {}
      this.connection = null;
      throw error;
    }
  }

  async stop(): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.stop();
    } finally {
      this.connection = null;
    }
  }
}


