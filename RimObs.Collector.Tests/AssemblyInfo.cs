using Xunit;

// The end-to-end smoke tests each boot a real WebApplication on a picked ephemeral port
// and a UDP receiver. Running them in parallel multiplies the chance of a port-bind race
// (see EndToEndSmokeTests.PickFreePort) and floods the test host with concurrent Kestrel
// instances. Serialize the whole assembly so each test owns the network surface alone.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
