# DNSPod IP Updater

This tool can help you update your DNSPod records.

## Configuration

It will use `DNSPodUpdater.json` by default, or you can specify a custom config file like this: `./DNSPodUpdater.exe new.json`. So you can open more than one Updater to update different domains.

And here is a sample JSON config file:

```
{
	"token": "ID,Token",
	"domain": "sohaj.in",
	"subdomain": [
		"cute",
		"princess"
	]
}
```

DNSPodUpdater will update each A records of the subdomains you set.

And what is `token`? You can find helps on DNSPod: [https://support.dnspod.cn/Kb/showarticle/tsid/227/](https://support.dnspod.cn/Kb/showarticle/tsid/227/)

## License

MIT