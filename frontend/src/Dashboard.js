import React from "react";
import axios from "axios";

function Dashboard() {
  const startInvestigators = async () => {
    try {
      await axios.post("https://localhost:5001/api/investigations/start");
      alert("🟢 Investigators started!");
    } catch (error) {
      alert("❌ Failed to start investigators.");
      console.error(error);
    }
  };

  const stopInvestigators = async () => {
    try {
      await axios.post("https://localhost:5001/api/investigations/stop");
      alert("🔴 Investigators stopped!");
    } catch (error) {
      alert("❌ Failed to stop investigators.");
      console.error(error);
    }
  };

  return (
    <div style={{ padding: "2rem", fontFamily: "Arial" }}>
      <h2>🧠 Intelligent E-Transformation Agent</h2>
      <button onClick={startInvestigators} style={{ marginRight: "1rem" }}>
        ▶️ Start Investigators
      </button>
      <button onClick={stopInvestigators}>⏹ Stop Investigators</button>
    </div>
  );
}

export default Dashboard;

