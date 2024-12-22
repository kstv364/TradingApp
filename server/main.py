from flask import Flask, request, jsonify
import yfinance as yf
from datetime import datetime
import time

app = Flask(__name__)

@app.route("/historical-data", methods=["GET"])
def get_historical_data():
    # Parse query parameters
    ticker = request.args.get("ticker")
    date_range = request.args.get("date_range", "1y")  # Default: 1y
    interval = request.args.get("interval", "1d")  # Default: 1d

    # Validate input
    if not ticker:
        return jsonify({"error": "Missing 'ticker' parameter."}), 400

    valid_date_ranges = ["1d", "5d", "1mo", "3mo", "6mo", "1y", "2y", "5y", "10y", "ytd", "max"]
    if date_range not in valid_date_ranges:
        return jsonify({"error": f"Invalid date range. Valid options are: {', '.join(valid_date_ranges)}"}), 400

    valid_intervals = ["1m", "2m", "5m", "15m", "30m", "60m", "90m", "1h", "1d", "5d", "1wk", "1mo", "3mo"]
    if interval not in valid_intervals:
        return jsonify({"error": f"Invalid interval. Valid options are: {', '.join(valid_intervals)}"}), 400

    # Fetch historical data with retries
    retries = 5
    for attempt in range(retries):
        try:
            stock = yf.Ticker(ticker)
            historical_data = stock.history(period=date_range, interval=interval)
            if historical_data.empty:
                return jsonify({"error": "No data found for the given parameters."}), 404

            # Format the data into a list of dictionaries
            data = historical_data.reset_index().to_dict(orient="records")
            return jsonify(data)

        except Exception as e:
            if attempt < retries - 1:
                time.sleep(2 ** attempt)  # Exponential backoff
            else:
                return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    app.run(debug=True)
