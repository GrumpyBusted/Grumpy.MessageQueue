# Grumpy.MessageQueue
API for Microsoft Message queue (MSMQ). This API extents the API with:
- Larger messages
- Transactional messages with Ack and NAck feature
- Queue Handler (Listener) to trigger handler method when messages are received
- Automatic management of queues (Create and Delete)

This API only includes a sub set of the features in MSMQ, but the features that are most used (and needed for my other solution).
The API makes the use of MSMQ easy and strait forward.