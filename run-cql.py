import sys
from cassandra.cluster import Cluster

arguments = sys.argv[1:]
if len(arguments) != 3:
    print("Usage: python " + sys.argv[0] + " <host> <port> <command>")
    sys.exit(1)

cluster = Cluster([arguments[0]], int(arguments[1]))
session = cluster.connect()
try:
    session.execute(arguments[2])
except Exception as ex:
    print("Exception while executing command: " + repr(ex))
    sys.exit(1)
else:
    print("Command has been executed successfully")
finally:
    cluster.shutdown()
