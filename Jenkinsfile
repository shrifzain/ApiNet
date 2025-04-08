pipeline {
    agent any  // Runs on any available Jenkins worker
    
    environment {
        // Basic settings
        PROJECT_NAME = 'ProNet.Api'              // Your app name
        SOLUTION_FILE = 'Api/Api.csproj'         // Your project file
        PUBLISH_DIR = 'publish'                  // Where the built app goes
        GITHUB_REPO = 'https://github.com/shrifzain/ApiNet.git'  // Your GitHub repo
        S3_BUCKET = 'pronet-artifacts'           // Your S3 bucket
        DEV_SERVER = '18.208.180.111'       // Your EC2 IP (replace this)
        SSH_KEY_ID = 'sheraa-ssh-key'            // Your SSH key in Jenkins
        AWS_KEY_ID = '01143717'           // Your AWS key in Jenkins
    }
    
    stages {
        // Step 1: Get code
        stage('Get Code') {
            steps {
                echo 'Getting code from GitHub'
                git url: 'https://github.com/shrifzain/infra.git', branch: 'master' ,credentialsId: '01143717'  // Pulls from main branch
                sh 'ls -la'  // Debug: See what files are here
                sh 'find . -name "*.csproj"'  // Debug: Find the project file
            }
        }
        
        // Step 2: Build app
        stage('Build') {
            steps {
                echo 'Building the app'
                sh 'dotnet publish Api.csproj -c Release -o ../${PUBLISH_DIR'
                }           
             }
        }
        
        // Step 3: Save to S3
        stage('Save to S3') {
            steps {
                echo 'Saving app to S3'
                withAWS(credentials: "${AWS_KEY_ID}") {
                    sh "aws s3 cp ${PUBLISH_DIR} s3://${S3_BUCKET}/${PROJECT_NAME}/ --recursive"
                    // Copies 'publish' folder to S3
                }
            }
        }
        
        // Step 4: Send to EC2
        stage('Deploy to Dev') {
            steps {
                echo 'Sending app to EC2'
                withCredentials([sshUserPrivateKey(credentialsId: "${SSH_KEY_ID}", keyFileVariable: 'SSH_KEY')]) {
                    sh """
                        ssh -i \$SSH_KEY ubuntu@${DEV_SERVER} 'mkdir -p /home/ubuntu/app'
                        scp -i \$SSH_KEY ${PUBLISH_DIR}/* ubuntu@${DEV_SERVER}:/home/ubuntu/app/
                        ssh -i \$SSH_KEY ubuntu@${DEV_SERVER} 'cd /home/ubuntu/app && nohup dotnet ProNet.Api.dll &'
                    """
                    // Sends app to EC2 and runs it
                }
            }
        }
    }
    
    // Clean up after
    post {
        always {
            echo 'Cleaning up'
            cleanWs()  // Deletes temporary files
        }
    }
}
