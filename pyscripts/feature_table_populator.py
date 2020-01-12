import arcgis
import arcpy
import csv
import json
import os
import sys
import tempfile
from urllib import request

url = 'https://opensky-network.org/api/states/all?lamin=33.87845&lomin=-117.81135&lamax=34.28221&lomax=-116.94345'
temp_dir = tempfile.mkdtemp()
file_path = os.path.join(temp_dir, 'latest_data.json')
response = request.urlretrieve(url, file_path)
json_file = open(file_path)
json_data = json.load(json_file)
states_data = json_data['states']

with open("test.csv", 'w+') as f:
   writer = csv.writer(f)
   header = ['icao24', 'callsign', 'origin_country', "time_position", "last_contact", "longitude", "latitude", "baro_altitude", "on_ground", "velocity", "true_track", "vertical_rate", "sensors", "geo_altitude", "squawk", "spi", "position_source"]
   writer.writerow(header)
   for row in states_data:
       writer.writerow(row)
