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
        private readonly Dictionary<Guid, Investigator> _investigators;
        private readonly Dictionary<Guid, List<InvestigatorResult>> _results = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationManager"/> class.
        /// </summary>
        /// <param name="investigators">The investigators to manage.</param>
        public InvestigationManager(IEnumerable<Investigator> investigators)
        {
            _investigators = investigators.ToDictionary(i => i.Id);
            foreach (var pair in _investigators)
            {
                var list = new List<InvestigatorResult>();
                _results[pair.Key] = list;
                pair.Value.Report = r => list.Add(r);
            }
        }

        /// <summary>
        /// Starts all registered investigators.
        /// </summary>
        public void StartAll()
        {
            foreach (var inv in _investigators.Values)
            {
                StartInvestigator(inv.Id);
            }
        }

        /// <summary>
        /// Stops all registered investigators.
        /// </summary>
        public void StopAll()
        {
            foreach (var inv in _investigators.Values)
            {
                StopInvestigator(inv.Id);
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
        public IEnumerable<object> GetAllInvestigatorStates()
        {
            return _investigators.Values.Select(i => new
            {
                i.Id,
                i.Name,
                i.IsRunning,
                ResultCount = _results[i.Id].Count
            });
        }

        /// <summary>
        /// Gets result logs for an investigator.
        /// </summary>
        public IEnumerable<InvestigatorResult> GetResults(Guid id)
        {
            return _results.TryGetValue(id, out var list) ? list : Enumerable.Empty<InvestigatorResult>();
        }
    }
}

