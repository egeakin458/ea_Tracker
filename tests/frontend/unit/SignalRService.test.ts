import { SignalRService, InvestigationEvents } from '../../../src/frontend/src/lib/SignalRService';
import * as signalR from '@microsoft/signalr';

// Mock the SignalR module
jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn(() => ({
    withUrl: jest.fn(() => ({
      withAutomaticReconnect: jest.fn(() => ({
        configureLogging: jest.fn(() => ({
          build: jest.fn(() => mockConnection)
        }))
      }))
    }))
  })),
  HubConnectionState: {
    Connected: 'Connected',
    Connecting: 'Connecting', 
    Disconnected: 'Disconnected'
  },
  LogLevel: {
    Information: 'Information'
  }
}));

// Mock connection object
const mockConnection = {
  start: jest.fn(),
  stop: jest.fn(),
  on: jest.fn(),
  onreconnecting: jest.fn(),
  onreconnected: jest.fn(),
  onclose: jest.fn(),
  state: 'Disconnected',
  serverTimeoutInMilliseconds: 30000,
  keepAliveIntervalInMilliseconds: 15000
};

describe('SignalRService', () => {
  let signalRService: SignalRService;
  let mockHandlers: InvestigationEvents;

  beforeEach(() => {
    signalRService = new SignalRService();
    mockHandlers = {
      onStarted: jest.fn(),
      onCompleted: jest.fn(), 
      onNewResult: jest.fn(),
      onStatusChanged: jest.fn(),
      onConnectionChange: jest.fn()
    };
    
    // Reset all mocks
    jest.clearAllMocks();
    mockConnection.state = 'Disconnected';
  });

  describe('start', () => {
    it('creates connection with correct hub URL', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(signalR.HubConnectionBuilder).toHaveBeenCalled();
      const builderInstance = (signalR.HubConnectionBuilder as jest.Mock).mock.results[0].value;
      expect(builderInstance.withUrl).toHaveBeenCalledWith('http://localhost:5050/hubs/investigations');
    });

    it('sets up event handlers for all investigation events', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(mockConnection.on).toHaveBeenCalledWith('InvestigationStarted', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('InvestigationCompleted', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('NewResultAdded', expect.any(Function));
      expect(mockConnection.on).toHaveBeenCalledWith('StatusChanged', expect.any(Function));
    });

    it('sets up reconnection handlers', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(mockConnection.onreconnecting).toHaveBeenCalledWith(expect.any(Function));
      expect(mockConnection.onreconnected).toHaveBeenCalledWith(expect.any(Function));
      expect(mockConnection.onclose).toHaveBeenCalledWith(expect.any(Function));
    });

    it('calls onConnectionChange with connecting state initially', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('connecting');
    });

    it('calls onConnectionChange with connected state on success', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('connected');
    });

    it('handles connection failure gracefully', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      const error = new Error('Connection failed');
      mockConnection.start.mockRejectedValue(error);
      mockConnection.stop.mockResolvedValue(undefined);

      // Act & Assert
      await expect(signalRService.start(baseUrl, mockHandlers)).rejects.toThrow('Connection failed');
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('disconnected');
    });

    it('skips start if already connected', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      
      // First start the connection normally
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Disconnected';
      await signalRService.start(baseUrl, mockHandlers);
      
      // Now simulate already connected state
      mockConnection.state = 'Connected';
      mockConnection.start.mockClear();

      // Act - try to start again
      await signalRService.start(baseUrl, mockHandlers);

      // Assert - start should not be called if already connected
      expect(mockConnection.start).not.toHaveBeenCalled();
    });

    it('configures connection timeouts for development', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      expect(mockConnection.serverTimeoutInMilliseconds).toBe(60000); // 60s
      expect(mockConnection.keepAliveIntervalInMilliseconds).toBe(15000); // 15s
    });

    it('removes trailing slash from base URL', async () => {
      // Arrange
      const baseUrl = 'http://localhost:5050/';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';

      // Act
      await signalRService.start(baseUrl, mockHandlers);

      // Assert
      const builderInstance = (signalR.HubConnectionBuilder as jest.Mock).mock.results[0].value;
      expect(builderInstance.withUrl).toHaveBeenCalledWith('http://localhost:5050/hubs/investigations');
    });
  });

  describe('stop', () => {
    it('stops connection when connection exists', async () => {
      // Arrange
      mockConnection.stop.mockResolvedValue(undefined);
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';
      
      await signalRService.start(baseUrl, mockHandlers);

      // Act
      await signalRService.stop();

      // Assert
      expect(mockConnection.stop).toHaveBeenCalled();
    });

    it('handles stop when no connection exists', async () => {
      // Act & Assert - should not throw
      await expect(signalRService.stop()).resolves.toBeUndefined();
    });

    it('cleans up connection reference after stop', async () => {
      // Arrange
      mockConnection.stop.mockResolvedValue(undefined);
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';
      
      await signalRService.start(baseUrl, mockHandlers);

      // Act
      await signalRService.stop();

      // Assert - next start should create new connection
      mockConnection.start.mockClear();
      await signalRService.start(baseUrl, mockHandlers);
      expect(signalR.HubConnectionBuilder).toHaveBeenCalledTimes(2);
    });
  });

  describe('event handling', () => {
    beforeEach(async () => {
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';
      await signalRService.start(baseUrl, mockHandlers);
    });

    it('handles InvestigationStarted event', () => {
      // Arrange
      const payload = { investigatorId: 'test-id', timestamp: '2023-01-01T00:00:00Z' };
      const onStartedHandler = mockConnection.on.mock.calls.find(call => call[0] === 'InvestigationStarted')[1];

      // Act
      onStartedHandler(payload);

      // Assert
      expect(mockHandlers.onStarted).toHaveBeenCalledWith(payload);
    });

    it('handles InvestigationCompleted event', () => {
      // Arrange
      const payload = { investigatorId: 'test-id', resultCount: 42, timestamp: '2023-01-01T00:00:00Z' };
      const onCompletedHandler = mockConnection.on.mock.calls.find(call => call[0] === 'InvestigationCompleted')[1];

      // Act
      onCompletedHandler(payload);

      // Assert
      expect(mockHandlers.onCompleted).toHaveBeenCalledWith(payload);
    });

    it('handles NewResultAdded event', () => {
      // Arrange
      const payload = { investigatorId: 'test-id', result: { id: 1, message: 'Test result' } };
      const onNewResultHandler = mockConnection.on.mock.calls.find(call => call[0] === 'NewResultAdded')[1];

      // Act
      onNewResultHandler(payload);

      // Assert
      expect(mockHandlers.onNewResult).toHaveBeenCalledWith(payload);
    });

    it('handles StatusChanged event', () => {
      // Arrange
      const payload = { investigatorId: 'test-id', newStatus: 'Running' };
      const onStatusChangedHandler = mockConnection.on.mock.calls.find(call => call[0] === 'StatusChanged')[1];

      // Act
      onStatusChangedHandler(payload);

      // Assert
      expect(mockHandlers.onStatusChanged).toHaveBeenCalledWith(payload);
    });
  });

  describe('reconnection handling', () => {
    beforeEach(async () => {
      const baseUrl = 'http://localhost:5050';
      mockConnection.start.mockResolvedValue(undefined);
      mockConnection.state = 'Connected';
      await signalRService.start(baseUrl, mockHandlers);
    });

    it('handles reconnecting event', () => {
      // Arrange
      const onReconnectingHandler = mockConnection.onreconnecting.mock.calls[0][0];

      // Act
      onReconnectingHandler();

      // Assert
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('connecting');
    });

    it('handles reconnected event', () => {
      // Arrange
      const onReconnectedHandler = mockConnection.onreconnected.mock.calls[0][0];

      // Act
      onReconnectedHandler();

      // Assert
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('connected');
    });

    it('handles connection close event', () => {
      // Arrange
      const onCloseHandler = mockConnection.onclose.mock.calls[0][0];
      const error = new Error('Connection lost');

      // Act
      onCloseHandler(error);

      // Assert
      expect(mockHandlers.onConnectionChange).toHaveBeenCalledWith('disconnected');
    });
  });

  describe('service instantiation', () => {
    it('can be instantiated without parameters', () => {
      // Act
      const service = new SignalRService();

      // Assert
      expect(service).toBeInstanceOf(SignalRService);
    });
  });
});