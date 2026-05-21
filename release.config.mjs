/** @type {import('semantic-release').GlobalConfig} */
export default {
    branches: ['main', { name: 'beta', prerelease: true }],
    plugins: [
        '@semantic-release/commit-analyzer',
        '@semantic-release/release-notes-generator',
        [
            'semantic-release-replace-plugin',
            {
                replacements: [
                    {
                        files: ['RimObs.Wire/BuildInfo.cs'],
                        from: 'Revision = ".*"',
                        to: 'Revision = "${nextRelease.version}"',
                        results: [{ file: 'RimObs.Wire/BuildInfo.cs', hasChanged: true, numMatches: 1, numReplacements: 1 }],
                        countMatches: true,
                    },
                    {
                        files: ['RimObs.Wire/BuildInfo.cs'],
                        from: 'BuildTime = ".*"',
                        to: () => `BuildTime = "${new Date().toISOString()}"`,
                        results: [{ file: 'RimObs.Wire/BuildInfo.cs', hasChanged: true, numMatches: 1, numReplacements: 1 }],
                        countMatches: true,
                    },
                ],
            },
        ],
        '@semantic-release/github',
        [
            '@semantic-release/git',
            {
                assets: ['RimObs.Wire/BuildInfo.cs'],
                message: 'chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}',
            },
        ],
    ],
};
