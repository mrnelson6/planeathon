import csv
import json
import os
import sys
import tempfile
from datetime import datetime, timedelta
from urllib import request

states_url = 'https://opensky-network.org/api/states/all?lamin=33.87845&lomin=-117.81135&lamax=34.28221&lomax=-116.94345'


states_url = 'https://opensky-network.org/api/states/all?lamin=33.87845&lomin=-117.81135&lamax=34.28221&lomax=-116.94345'
temp_dir = tempfile.mkdtemp()
file_path = os.path.join(temp_dir, 'latest_data.json')
response = request.urlretrieve(states_url, file_path)
json_file = open(file_path)
json_data = json.load(json_file)
states_data = json_data['states']
first_callsign = states_data[0][1]
flight_number = first_callsign.strip()[2:]
print(states_data)

now = datetime.now()
now_unix = str(int(datetime.timestamp(now)))
before = now - timedelta(30)
before_unix = str(int(datetime.timestamp(before)))
print(flight_number)

# flights_by_icao_url = "http://api.aviationstack.com/v1/flights?access_key=31b25aae611375628fa91862098b8d6a&flight_number=" + flight_number + "&flight_status=active"

# flights_file_path = os.path.join(temp_dir, 'flights_data.json')
# response = request.urlretrieve(flights_by_icao_url, flights_file_path)
# flights_json_file = open(flights_file_path)
# flights_json_data = json.load(flights_json_file)
# print(flights_json_data)
