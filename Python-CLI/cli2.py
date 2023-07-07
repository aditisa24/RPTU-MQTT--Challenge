import sys
import time
import paho.mqtt.client as mqtt

# MQTT broker details
broker = "127.0.0.1" #Please provide the IP where the server(mosquitto) is running
port = 1883 #Please provide the port number of mosquitto

# MQTT topics
publish_topic = "movement"
subscribe_topic = "information"

def on_connect(client, userdata, flags, rc):
    if rc == 0:
        print("Connected to MQTT broker")
        # Subscribe to the "information" topic upon successful connection
        client.subscribe(subscribe_topic)
    else:
        print("Failed to connect, return code:", rc)
        sys.exit(1)

def on_publish(client, userdata, mid):
    print("Message sent to movement topic")

def on_message(client, userdata, msg):
    received_message = msg.payload.decode("utf-8")
    print("Received message from information topic:", received_message)
    # Perform additional actions with the received message if needed

def on_disconnect(client, userdata, rc):
    print("Disconnected from MQTT broker")

# Create an MQTT client instance
client = mqtt.Client()

# Assign callback functions
client.on_connect = on_connect
client.on_publish = on_publish
client.on_message = on_message
client.on_disconnect = on_disconnect

# Connect to the MQTT broker
client.connect(broker, port)

# Loop to maintain network traffic flow
client.loop_start()

# Wait for a while to establish the connection
time.sleep(1)

print("Publish a message to the 'movement' topic (Press 'Esc' to exit):")

while True:
    message = input()

    # Check if 'Esc' key is pressed
    if message == '\x1b':
        break

    # Publish the message to the topic
    client.publish(publish_topic, message)

# Disconnect from the MQTT broker
    client.loop_stop()
client.disconnect()
