pipeline {
  agent { docker { image 'microsoft/dotnet:2.1-sdk-alpine' } }
  options {
    buildDiscarder(logRotator(numToKeepStr:'10'))
  }
  environment {
    HOME = "${WORKSPACE}"
  }
  stages {
    stage('Build') {
      steps {
        sh 'dotnet publish src/ -c Release -o out'
      }
    }

    stage('Create Nuget Artifact') {
      environment {
        DEFAULT_VERSION = '1.0.0'
      }
      stages {
        stage ('Compile Pre-release Version') {
          when { not { buildingTag() } }
          steps {
            script {
              def alphaSuffix = VersionNumber versionNumberString: '${BUILD_YEAR}-${BUILD_MONTH, XX}-${BUILD_DAY, XX}b${BUILDS_TODAY, XX}', versionPrefix: 'alpha.', worstResultForIncrement: 'SUCCESS'
              env.PACKAGE_VERSION = nextPreReleaseVersion(env.DEFAULT_VERSION, alphaSuffix)
              currentBuild.description = env.PACKAGE_VERSION
            }
          }
        }
        stage ('Set Tagged Version') {
          when { buildingTag() }
          steps {
            script {
              env.PACKAGE_VERSION = env.BRANCH_NAME
            }
          }
        }
        stage ('Pack and Archive') {
          steps {
            sh "dotnet pack src/ /p:PackageVersion=${PACKAGE_VERSION} -c Release -o out --no-build"
            archiveArtifacts 'src/**/out/*.nupkg'
          }
        }
        stage ('Publish on Nuget') {
          when { anyOf { branch 'master'; buildingTag() } }
          steps {
            script {
              def packages = findFiles glob: 'src/**/out/*.nupkg'
              packages.each {
                sh "dotnet nuget push ${it.path} -s https://nuget.factioninc.com/v3/index.json"
              }
            }
          }
        }
      }
      post {
        always { script { slack.noArtifact() } }
      }
    }
  }
}