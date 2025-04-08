pipeline {
    agent any
    
    environment {
        PROJECT_NAME = 'ProNet.Api'
        SOLUTION_FILE = 'Api.csproj'  // We’ll verify if this is correct
        PUBLISH_DIR = 'publish'
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'
        S3_BUCKET = 'pronet-artifacts'
        DEV_SERVER = '18.208.180.111'
        SSH_KEY_ID = 'sheraa-ssh-key'
        AWS_KEY_ID = 'awss'
    }
    
    stages {
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: "${GITHUB_REPO}", branch: 'master', credentialsId: '01143717'
                sh 'ls -la'  // List root directory
                sh 'find . -type f'  // List all files in the repo to see full structure
                sh 'find . -name "*.csproj"'  // Find exact .csproj location
            }
        }
        
        stage('Build') {
            steps {
                echo 'Building the app'
                sh "dotnet publish ${SOLUTION_FILE} -c Release -o ${PUBLISH_DIR}"
            }
        }
        
        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"
                }
            }
        }
        
        stage('Deploy to Dev') {
            steps {
                echo 'Sending app to EC2'
                withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i \$SSH_KEY ubuntu@${DEV_SERVER} 'mkdir -p /home/ubuntu/app'
                        scp -i \$SSH_KEY ${PUBLISH_DIR}/* ubuntu@${DEV_SERVER}:/home/ubuntu/app/
                        ssh -i \$SSH_KEY ubuntu@${DEV_SERVER} 'cd /home/ubuntu/app && nohup dotnet ProNet.Api.dll &'
                    """
                }
            }
        }
    }
    
    post {
        always {
            echo 'Cleaning up'
            cleanWs()
        }
    }
}
