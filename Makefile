.PHONY: help all clean restore build build-library build-collector build-dashboard test format lint watch publish-collector

CONFIG ?= Debug
RIDS ?= win-x64 linux-x64 osx-arm64 osx-x64
DASHBOARD_DIR := RimObs.Dashboard
SLN := RimObs.sln

help:
	@echo "Targets:"
	@echo "  all                 restore, build dashboard, build solution"
	@echo "  clean               remove bin/, obj/, Assemblies/, dashboard dist/"
	@echo "  restore             dotnet restore + pnpm install"
	@echo "  build               build the whole solution"
	@echo "  build-library       build only RimObs.Library"
	@echo "  build-collector     build only RimObs.Collector"
	@echo "  build-dashboard     build the Svelte SPA into dist/"
	@echo "  test                run xUnit suites"
	@echo "  format              dotnet format + prettier"
	@echo "  lint                dotnet format --verify-no-changes + eslint"
	@echo "  watch               dotnet watch on the collector"
	@echo "  publish-collector   self-contained publish for all RIDs"

all: restore build-dashboard build

clean:
	rm -rf Assemblies/*.dll Assemblies/*.pdb
	rm -rf RimObs.Library/bin RimObs.Library/obj
	rm -rf RimObs.Library.Tests/bin RimObs.Library.Tests/obj
	rm -rf RimObs.Wire/bin RimObs.Wire/obj
	rm -rf RimObs.Collector/bin RimObs.Collector/obj
	rm -rf RimObs.Collector.Tests/bin RimObs.Collector.Tests/obj
	rm -rf $(DASHBOARD_DIR)/dist

restore:
	dotnet restore $(SLN)
	cd $(DASHBOARD_DIR) && pnpm install --frozen-lockfile || pnpm install

build: build-dashboard
	dotnet build $(SLN) -c $(CONFIG) --nologo

build-library:
	dotnet build RimObs.Library/RimObs.Library.csproj -c $(CONFIG) --nologo

build-collector: build-dashboard
	dotnet build RimObs.Collector/RimObs.Collector.csproj -c $(CONFIG) --nologo

build-dashboard:
	cd $(DASHBOARD_DIR) && pnpm build

test:
	dotnet test $(SLN) -c $(CONFIG) --nologo --no-build

format:
	dotnet format $(SLN)
	cd $(DASHBOARD_DIR) && pnpm format || true

lint:
	dotnet format $(SLN) --verify-no-changes
	cd $(DASHBOARD_DIR) && pnpm lint || true

watch:
	dotnet watch --project RimObs.Collector run -- serve

publish-collector: build-dashboard
	@for rid in $(RIDS); do \
		echo "==> publish $$rid"; \
		dotnet publish RimObs.Collector/RimObs.Collector.csproj \
			-c Release -r $$rid --self-contained true \
			-p:PublishSingleFile=true \
			-o out/collector/$$rid \
			--nologo; \
	done
