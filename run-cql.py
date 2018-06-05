import sys
from cassandra.cluster import Cluster

arguments = sys.argv[1:]
if len(arguments) != 2:
    print("Usage: python " + sys.argv[0] + " <host> <command>")
    sys.exit(1)

cluster = Cluster([arguments[0]])
session = cluster.connect()
try:
    session.execute(arguments[1])
except Exception as ex:
    print("Exception while executing command: " + repr(ex))
    sys.exit(1)
else:
    print("Command has been executed successfully")
finally:
    cluster.shutdown()
