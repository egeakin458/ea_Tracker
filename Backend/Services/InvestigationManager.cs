using System;
using System.Collections.Generic;
using System.Linq;
using ea_Tracker.Models;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Manages a collection of investigators and coordinates their lifecycle.
    /// </summary>
    public class InvestigationManager
    {
        private readonly IInvestigatorFactory _factory;
        private readonly Dictionary<Guid, Investigator> _investigators = new();
        private readonly Dictionary<Guid, List<InvestigatorResult>> _results = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationManager"/> class.
        /// </summary>
        public InvestigationManager(IInvestigatorFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Starts all registered investigators.
        /// </summary>
        public void StartAll()
        {
            foreach (var id in _investigators.Keys)
            {
                StartInvestigator(id);
            }
        }

        /// <summary>
        /// Stops all registered investigators.
        /// </summary>
        public void StopAll()
        {
            foreach (var id in _investigators.Keys)
            {
                StopInvestigator(id);
            }
        }

        /// <summary>
        /// Starts a single investigator.
        /// </summary>
        public void StartInvestigator(Guid id)
        {
            if (_investigators.TryGetValue(id, out var inv))
            {
                inv.Start();
            }
        }

        /// <summary>
        /// Stops a single investigator.
        /// </summary>
        public void StopInvestigator(Guid id)
        {
            if (_investigators.TryGetValue(id, out var inv))
            {
                inv.Stop();
            }
        }

        /// <summary>
        /// Gets the state of all investigators.
        /// </summary>
        public IEnumerable<ea_Tracker.Models.Dtos.InvestigatorStateDto> GetAllInvestigatorStates()
            => _investigators.Values.Select(i => new ea_Tracker.Models.Dtos.InvestigatorStateDto(
                   i.Id,
                   i.Name,
                   i.IsRunning,
                   _results[i.Id].Count));

        /// <summary>
        /// Gets result logs for an investigator.
        /// </summary>
        public IEnumerable<ea_Tracker.Models.Dtos.InvestigatorResultDto> GetResults(Guid id)
            => _results.TryGetValue(id, out var list)
               ? list.Select(r => new ea_Tracker.Models.Dtos.InvestigatorResultDto(r.InvestigatorId, r.Timestamp, r.Message!, r.Payload))
               : Enumerable.Empty<ea_Tracker.Models.Dtos.InvestigatorResultDto>();

        /// <summary>
        /// Creates a new investigator of the specified kind and registers it.
        /// </summary>
        public Guid CreateInvestigator(string kind)
        {
            var inv = _factory.Create(kind);
            _investigators[inv.Id] = inv;
            var list = new List<InvestigatorResult>();
            _results[inv.Id] = list;
            inv.Report = r => list.Add(r);
            return inv.Id;
        }
    }
}
