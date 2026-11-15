"""Small analysis of the interaction logs written by InteractionLogger.cs.

Usage:
    python analyze_logs.py path/to/logs_folder
"""
import sys
from pathlib import Path

import pandas as pd


def main(folder: str) -> None:
    files = sorted(Path(folder).glob("session_*.csv"))
    if not files:
        print("No session_*.csv files found in", folder)
        return

    df = pd.concat([pd.read_csv(f) for f in files], ignore_index=True)
    df["tools_used"] = df["tools_used"].fillna("")

    print(f"Sessions: {len(files)} | Interactions: {len(df)}")
    print("\nResponse time (ms) per participant:")
    print(df.groupby("participant_id")["response_time_ms"].describe()[["count", "mean", "min", "max"]].round(0))

    tools = df["tools_used"].str.split("|").explode()
    tools = tools[tools != ""]
    print("\nTool usage:")
    print(tools.value_counts() if not tools.empty else "no tools used")

    df["user_words"] = df["user_input"].fillna("").str.split().str.len()
    df["agent_words"] = df["agent_response"].fillna("").str.split().str.len()
    print("\nAverage words per message (user / agent):",
          round(df["user_words"].mean(), 1), "/", round(df["agent_words"].mean(), 1))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else ".")
